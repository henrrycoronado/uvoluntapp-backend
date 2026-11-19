using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Seeders;

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

        var demoPassword = configuration["DEV_USERS_PASSWORD"];
        if (!string.IsNullOrWhiteSpace(demoPassword))
        {
            await EnsureRoleCombinationUsersAsync(scope.ServiceProvider, demoPassword, cancellationToken);
        }
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

    /// <summary>
    /// Usuarios de prueba con combinaciones de roles (solo Development). Comparten DEV_USERS_PASSWORD.
    /// </summary>
    private static async Task EnsureRoleCombinationUsersAsync(
        IServiceProvider provider,
        string password,
        CancellationToken cancellationToken)
    {
        var db = provider.GetRequiredService<AppDbContext>();
        var programs = await db.Programs.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        var programA = programs.FirstOrDefault(p => p.Status == ProgramStatus.ACTIVE) ?? programs.FirstOrDefault();
        var programB = programs.FirstOrDefault(p => p.Id != programA?.Id);
        if (programA is null)
        {
            return;
        }

        var admin = await db.Users.Where(u => u.Status == AccountStatus.ACTIVE)
            .Join(db.RoleAssignments.Where(r => r.Role == AssignedRole.ADMIN && r.RevokedAt == null), u => u.Id, r => r.UserId, (u, _) => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        (string Email, string FirstName, (AssignedRole Role, Guid? Program)[] Roles)[] people =
        [
            ("voluntario@uvoluntapp.test", "Valeria", []),
            ("lider.programa@uvoluntapp.test", "Lucas", [(AssignedRole.PROGRAM_LEADER, programA.Id)]),
            ("lider.operativo@uvoluntapp.test", "Olivia", [(AssignedRole.OPERATIONAL_LEADER, programA.Id)]),
            ("lider.sinprograma@uvoluntapp.test", "Sergio", [(AssignedRole.PROGRAM_LEADER, null)]),
            ("lider.combinado@uvoluntapp.test", "Camila", programB is null
                ? [(AssignedRole.PROGRAM_LEADER, programA.Id)]
                : [(AssignedRole.PROGRAM_LEADER, programA.Id), (AssignedRole.OPERATIONAL_LEADER, programB.Id)]),
        ];

        foreach (var person in people)
        {
            await EnsureTestUserAsync(provider, person.Email, password, person.FirstName, cancellationToken);
            var userId = await db.Users.Where(u => u.NormalizedEmail == person.Email.ToUpperInvariant()).Select(u => u.Id).FirstAsync(cancellationToken);
            foreach (var (role, program) in person.Roles)
            {
                var scope = program is null ? RoleScope.GLOBAL : RoleScope.PROGRAM;
                var exists = await db.RoleAssignments.AnyAsync(
                    r => r.UserId == userId && r.Role == role && r.ScopeId == program && r.RevokedAt == null,
                    cancellationToken);
                if (!exists)
                {
                    db.RoleAssignments.Add(RoleAssignment.Assign(userId, role, scope, program, admin == Guid.Empty ? userId : admin));
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task EnsureTestUserAsync(
        IServiceProvider provider,
        string email,
        string password,
        string firstName,
        CancellationToken cancellationToken)
    {
        var db = provider.GetRequiredService<AppDbContext>();
        var users = provider.GetRequiredService<UserManager<AppUser>>();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == email.ToUpperInvariant(), cancellationToken))
        {
            return;
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Status = AccountStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow,
        };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                "No se pudo crear el usuario de prueba: " + string.Join(' ', created.Errors.Select(e => e.Description)));
        }

        db.Profiles.Add(new Profile { UserId = user.Id, FirstName = firstName, LastName = "Prueba" });
        await db.SaveChangesAsync(cancellationToken);
    }
}
