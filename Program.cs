using U_VoluntApp_Core.Src.Presentation.Extensions;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAppServices(builder.Configuration);
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
public partial class Program { }
