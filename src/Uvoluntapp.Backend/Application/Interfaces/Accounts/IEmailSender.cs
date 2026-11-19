namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken);
}
