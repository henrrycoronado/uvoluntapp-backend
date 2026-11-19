using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Application.Accounts.Approvals;
using Uvoluntapp.Backend.Application.Accounts.Email;
using Uvoluntapp.Backend.Application.Accounts.Google;
using Uvoluntapp.Backend.Application.Accounts.Recovery;
using Uvoluntapp.Backend.Application.Accounts.Suspension;
using Uvoluntapp.Backend.Application.Administration.Ports;
using Uvoluntapp.Backend.Application.Administration.Services;
using Uvoluntapp.Backend.Application.Audit;
using Uvoluntapp.Backend.Application.Authorization;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Application.Profiles;
using Uvoluntapp.Backend.Application.Profiles.Ports;
using Uvoluntapp.Backend.Application.Profiles.Services;
using Uvoluntapp.Backend.Application.Programs.Ports;
using Uvoluntapp.Backend.Application.Programs.Services;
using Uvoluntapp.Backend.Infrastructure.Accounts;
using Uvoluntapp.Backend.Infrastructure.Administration;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;
using Uvoluntapp.Backend.Infrastructure.Programs;
using Uvoluntapp.Backend.Infrastructure.Seeding;
using Uvoluntapp.Backend.Presentation.Accounts;
using Uvoluntapp.Backend.Presentation.Errors;

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
        services.AddScoped<IProgramReader, ProgramReader>();

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
        services.AddScoped<GoogleAuthService>();
        services.AddScoped<RoleAssignmentService>();
        services.AddScoped<AdminGrantService>();
        services.AddScoped<AccountApprovalAdminService>();
        services.AddScoped<UserAdministrationService>();
        services.AddScoped<ProfileApplicationService>();
        services.AddScoped<ProgramQueryService>();

        var allowedOrigins = (configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:5173")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        services.AddCors(options => options.AddPolicy("web", policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        services.AddDataProtection();
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddControllers();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => AccountBearer.Configure(options, configuration));
        services.AddAuthorization();
        return services;
    }
}
