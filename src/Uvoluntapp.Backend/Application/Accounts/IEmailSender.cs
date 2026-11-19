namespace Uvoluntapp.Backend.Application.Accounts;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken);
}
