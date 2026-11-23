using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Audit;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Interfaces.Profiles;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Application.Services.Accounts;
using Uvoluntapp.Backend.Application.Services.Activities;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Application.Services.Authorization;
using Uvoluntapp.Backend.Application.Services.Common;
using Uvoluntapp.Backend.Application.Services.Profiles;
using Uvoluntapp.Backend.Application.Services.Programs;
using Uvoluntapp.Backend.Infrastructure.Auth;
using Uvoluntapp.Backend.Infrastructure.Email;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Jobs;
using Uvoluntapp.Backend.Infrastructure.Persistence;
using Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;
using Uvoluntapp.Backend.Infrastructure.Persistence.Seeders;
using Uvoluntapp.Backend.Infrastructure.Storage;
using Uvoluntapp.Backend.Presentation.Helpers;
using Uvoluntapp.Backend.Presentation.Middleware;

namespace Uvoluntapp.Backend.Presentation.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration["DB_CONNECTION_STRING"];
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Falta la cadena de conexión a PostgreSQL. Configure ConnectionStrings:DefaultConnection o DB_CONNECTION_STRING.");
        }

        // Persistencia y identidad.
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddIdentityCore<AppUser>(AccountIdentityOptions.Configure).AddEntityFrameworkStores<AppDbContext>();
        services.AddHostedService<DatabaseInitializer>();

        // Después de las migraciones: inicia las actividades publicadas al llegar su horario (SCRUM-50).
        services.AddHostedService<ActivityStartWorker>();

        // Infraestructura externa.
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(
            configuration["REDIS_CONNECTION_STRING"] ?? throw new InvalidOperationException("Falta REDIS_CONNECTION_STRING.")));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AccountRateLimiter>();
        services.AddSingleton<IAccountRateLimiter>(sp => sp.GetRequiredService<AccountRateLimiter>());
        services.AddSingleton<RegistrationCodes>();
        services.AddSingleton<SessionTokens>();
        services.AddSingleton<RegistrationProof>();
        services.AddSingleton<IOneTimeCodeGenerator, OneTimeCodeGenerator>();
        services.AddSingleton<IRecoveryTokenGenerator, RecoveryTokenGenerator>();
        services.AddHttpClient<IEmailSender, RegistrationEmailSender>();
        services.AddHttpClient<IGoogleTokenValidator, GoogleTokenValidator>();

        // Puertos de persistencia (un store por agregado).
        services.AddScoped<IRegistration, Registration>();
        services.AddScoped<IAccountSessions, AccountSessions>();
        services.AddScoped<ISessionIssuer, SessionIssuer>();
        services.AddScoped<IRoleAssignmentStore, RoleAssignmentStore>();
        services.AddScoped<IAdminGrantStore, AdminGrantStore>();
        services.AddScoped<IAccountDirectory, AccountDirectory>();
        services.AddScoped<IAccountApprovalStore, AccountApprovalStore>();
        services.AddScoped<IAccountSuspensionStore, AccountSuspensionStore>();
        services.AddScoped<IAccountEmailStore, AccountEmailStore>();
        services.AddScoped<IGoogleAccountStore, GoogleAccountStore>();
        services.AddScoped<IRecoveryAccountStore, RecoveryAccountStore>();
        services.AddScoped<IProfileStore, ProfileStore>();
        services.AddScoped<IAuditEventStore, AuditEventStore>();
        services.AddScoped<IProgramStore, ProgramStore>();
        services.AddScoped<IAcademicPeriodStore, AcademicPeriodStore>();
        services.AddScoped<IActivityStore, ActivityStore>();

        // Reglas de aplicación sin estado.
        services.AddSingleton<ContextAuthorizationService>();
        services.AddSingleton<ProfileService>();
        services.AddScoped<AdminAccessGuard>();
        services.AddScoped<AuditRecorder>();
        services.AddScoped<AuditQueryService>();
        services.AddScoped<AccountEmailService>();
        services.AddScoped<AccountSuspensionService>();
        services.AddScoped<AccountApprovalService>();
        services.AddScoped<RecoveryService>();
        services.AddSingleton<IWebLinks, WebLinks>();
        services.AddScoped<GoogleAuthService>();
        services.AddScoped<RoleAssignmentService>();
        services.AddScoped<AdminGrantService>();
        services.AddScoped<AccountApprovalAdminService>();
        services.AddScoped<UserAdministrationService>();
        services.AddScoped<ProfileApplicationService>();
        services.AddScoped<PlanningAccess>();
        services.AddScoped<ProgramService>();
        services.AddScoped<PeriodAdministration>();
        services.AddScoped<ResourceImageService>();
        services.AddScoped<IMediaStore, MediaStore>();
        services.AddSingleton<IImageProcessor, ImageProcessor>();
        services.AddSingleton<IFileStorage, R2FileStorage>();
        services.AddScoped<ActivityNotices>();
        services.AddScoped<ActivityService>();
        services.AddScoped<EnrollmentService>();
        services.AddScoped<IEnrollmentStore, EnrollmentStore>();
        services.AddScoped<ActivityStarter>();
        services.AddScoped<CatalogService>();

        var allowedOrigins = (configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        services.AddCors(options => options.AddPolicy("web", policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        services.AddDataProtection();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions.TryAdd("code", $"HTTP_{context.ProblemDetails.Status}"));
        services.AddSingleton<PlanningRejectionAudit>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddControllers();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => AccountBearer.Configure(options, configuration));
        services.AddAuthorization();
        return services;
    }
}
