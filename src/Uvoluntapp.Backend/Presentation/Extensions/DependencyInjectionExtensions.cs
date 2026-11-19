using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Infrastructure.Accounts;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;
using Uvoluntapp.Backend.Presentation.Accounts;

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

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddIdentityCore<AppUser>(AccountIdentityOptions.Configure).AddEntityFrameworkStores<AppDbContext>();
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(
            configuration["REDIS_CONNECTION_STRING"] ?? throw new InvalidOperationException("Falta REDIS_CONNECTION_STRING.")));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AccountRateLimiter>();
        services.AddSingleton<RegistrationCodes>();
        services.AddSingleton<SessionTokens>();
        services.AddSingleton<RegistrationProof>();
        services.AddScoped<IRegistration, Registration>();
        services.AddScoped<IAccountSessions, AccountSessions>();
        services.AddHttpClient<IEmailSender, RegistrationEmailSender>();
        services.AddDataProtection();
        services.AddProblemDetails();
        services.AddControllers();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => AccountBearer.Configure(options, configuration));
        services.AddAuthorization();
        return services;
    }
}
