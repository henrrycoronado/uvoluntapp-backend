using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Audit;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Seeders;

/// <summary>
/// Aplica las migraciones al arrancar y crea los dos administradores iniciales (SCRUM-106) en cualquier entorno,
/// con FOUNDER_ADMIN_1_* y FOUNDER_ADMIN_2_* (o DEV_ADMIN_* en local). Es idempotente: si ya existen, no crea otros.
/// Los programas y usuarios de prueba solo se crean en Development.
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

        await EnsureFounderAdminsAsync(scope.ServiceProvider, cancellationToken);

        if (!environment.IsDevelopment())
        {
            return;
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

    private static async Task EnsureAdminAsync(
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
                    "No se pudo crear el administrador inicial: " + string.Join(' ', created.Errors.Select(e => e.Description)));
            }

            db.Profiles.Add(new Profile { UserId = user.Id, FirstName = firstName, LastName = "Pastoral" });
            db.AuditEvents.Add(AuditEvent.CreateSystem(
                AuditActions.FounderAdminProvisioned,
                AuditResourceTypes.User,
                user.Id,
                new Dictionary<string, object?> { ["email"] = user.Email }));
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

    /// <summary>
    /// Exactamente dos administradores iniciales. Sin los dos configurados no crea ninguno; con los dos ya creados no hace nada.
    /// </summary>
    private async Task EnsureFounderAdminsAsync(IServiceProvider provider, CancellationToken cancellationToken)
    {
        var founders = Enumerable.Range(1, 2)
            .Select(index => (
                Index: index,
                Email: configuration[$"FOUNDER_ADMIN_{index}_EMAIL"] ?? configuration[$"DEV_ADMIN_{index}_EMAIL"],
                Password: configuration[$"FOUNDER_ADMIN_{index}_PASSWORD"] ?? configuration[$"DEV_ADMIN_{index}_PASSWORD"]))
            .ToList();
        if (founders.Any(f => string.IsNullOrWhiteSpace(f.Email) || string.IsNullOrWhiteSpace(f.Password)))
        {
            logger.LogWarning("Administradores iniciales sin configurar: define FOUNDER_ADMIN_1_* y FOUNDER_ADMIN_2_*.");
            return;
        }

        if (string.Equals(founders[0].Email!.Trim(), founders[1].Email!.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Los dos administradores iniciales deben tener correos distintos.");
        }

        // Una vez creados los dos, cambiar la configuración no crea más administradores.
        var db = provider.GetRequiredService<AppDbContext>();
        var provisioned = await db.AuditEvents.CountAsync(e => e.Action == AuditActions.FounderAdminProvisioned, cancellationToken);
        if (provisioned >= 2)
        {
            return;
        }

        foreach (var founder in founders)
        {
            await EnsureAdminAsync(provider, founder.Email!, founder.Password!, $"Administrador {founder.Index}", cancellationToken);
        }
    }
}
