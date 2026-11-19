namespace Uvoluntapp.Backend.Application.Interfaces.Common;

/// <summary>Enlaces a pantallas de la web que se envían por correo.</summary>
public interface IWebLinks
{
    string PasswordReset(string token);

    string CreatePassword(string email);
}
