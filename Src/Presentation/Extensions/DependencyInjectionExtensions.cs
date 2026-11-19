using Microsoft.EntityFrameworkCore;
using U_VoluntApp_Core.Src.Infrastructure.Persistence;

namespace U_VoluntApp_Core.Src.Presentation.Extensions;

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

        return services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    }
}
