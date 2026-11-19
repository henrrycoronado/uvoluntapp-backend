using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Domain.Programs;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Seeding;

/// <summary>
/// Aplica las migraciones al arrancar. En Development crea dos administradores de prueba
/// (DEV_ADMIN_1_* y DEV_ADMIN_2_*). Nunca se ejecuta fuera de Development.
/// </summary>
public sealed class DatabaseInitializer(
    IServiceProvider services,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<DatabaseInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("DATABASE_AUTO_MIGRATE", true))
        {
            return;
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migraciones aplicadas.");

        if (!environment.IsDevelopment())
        {
            return;
        }

        for (var index = 1; index <= 2; index++)
        {
            var email = configuration[$"DEV_ADMIN_{index}_EMAIL"];
            var password = configuration[$"DEV_ADMIN_{index}_PASSWORD"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            await EnsureTestAdminAsync(scope.ServiceProvider, email, password, $"Admin{index}", cancellationToken);
        }

        await EnsureDemoProgramsAsync(db, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Programas de demostración para probar asignaciones; la gestión real llega con el módulo 2.</summary>
    private static async Task EnsureDemoProgramsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Programs.AnyAsync(cancellationToken))
        {
            return;
        }

        var community = new VolunteerProgram
        {
            Name = "Voluntariado comunitario",
            Description = "Apoyo a comunidades y barrios de Santa Cruz.",
        };
        community.Activate();
        db.Programs.Add(community);
        db.Programs.Add(new VolunteerProgram
        {
            Name = "Tutorías académicas",
            Description = "Acompañamiento a estudiantes de primeros semestres.",
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureTestAdminAsync(
        IServiceProvider provider,
        string email,
        string password,
        string firstName,
        CancellationToken cancellationToken)
    {
        var db = provider.GetRequiredService<AppDbContext>();
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        var normalized = email.Trim().ToUpperInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email.Trim(),
                Email = email.Trim(),
                EmailConfirmed = true,
                Status = AccountStatus.ACTIVE,
                CreatedAt = DateTime.UtcNow,
            };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    "No se pudo crear el administrador de prueba: " + string.Join(' ', created.Errors.Select(e => e.Description)));
            }

            db.Profiles.Add(new Profile { UserId = user.Id, FirstName = firstName, LastName = "Prueba" });
        }

        var hasAdmin = await db.RoleAssignments.AnyAsync(
            r => r.UserId == user.Id && r.Role == AssignedRole.ADMIN && r.Scope == RoleScope.GLOBAL && r.RevokedAt == null,
            cancellationToken);
        if (!hasAdmin)
        {
            db.RoleAssignments.Add(RoleAssignment.Assign(user.Id, AssignedRole.ADMIN, RoleScope.GLOBAL, null, user.Id));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
