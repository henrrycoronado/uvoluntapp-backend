using Uvoluntapp.Backend.Presentation.Extensions;

namespace Uvoluntapp.Backend;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddAppServices(builder.Configuration);
        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ShortCircuit();
        app.Run();
    }
}
