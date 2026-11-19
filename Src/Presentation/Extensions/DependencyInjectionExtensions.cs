using Microsoft.EntityFrameworkCore;
using U_VoluntApp_Core.Src.Infrastructure.Persistence;
namespace U_VoluntApp_Core.Src.Presentation.Extensions;
public static class DependencyInjectionExtensions { public static IServiceCollection AddAppServices(this IServiceCollection s,IConfiguration c){s.AddDbContext<AppDbContext>(o=>o.UseNpgsql(c.GetConnectionString("DefaultConnection")));return s;} }
