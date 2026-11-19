using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using Uvoluntapp.Backend.Application.Accounts;

namespace Uvoluntapp.Backend.Infrastructure.Accounts;

public sealed class RegistrationEmailSender(HttpClient client, IConfiguration config, IWebHostEnvironment environment) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string message, CancellationToken cancellationToken)
    {
        var from = config["EMAIL_FROM"] ?? throw new InvalidOperationException("Falta EMAIL_FROM.");
        if (environment.IsDevelopment())
        {
            using var smtp = new SmtpClient(config["MAILPIT_HOST"] ?? "localhost", config.GetValue("MAILPIT_SMTP_PORT", 1025));
            using var mail = new MailMessage(from, recipient, subject, message);
            await smtp.SendMailAsync(mail, cancellationToken);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new { from, to = new[] { recipient }, subject, text = message })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", config["RESEND_API_KEY"] ?? throw new InvalidOperationException("Falta RESEND_API_KEY."));
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
