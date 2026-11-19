using Uvoluntapp.Backend.Application.Interfaces.Common;

namespace Uvoluntapp.Backend.Infrastructure.Email;

/// <summary>Arma los enlaces con WEB_BASE_URL (por defecto la web local).</summary>
public sealed class WebLinks(IConfiguration configuration) : IWebLinks
{
    private string BaseUrl => (configuration["WEB_BASE_URL"] ?? "http://localhost:5173").TrimEnd('/');

    public string PasswordReset(string token) => $"{BaseUrl}/restablecer?token={Uri.EscapeDataString(token)}";

    public string CreatePassword(string email) => $"{BaseUrl}/crear-contrasena?correo={Uri.EscapeDataString(email)}";
}
