using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.Services.Activities;

public sealed class ActivityNotices(IEnrollmentStore enrollments, IEmailSender mail, ILogger<ActivityNotices> logger)
{
    public async Task SendAsync(Activity activity, string message, CancellationToken ct)
    {
        try
        {
            foreach (var email in await enrollments.RecipientsAsync(activity.Id, ct))
            {
                try
                {
                    await mail.SendAsync(email, activity.Title, message, ct);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Falló un aviso de actividad {ActivityId}", activity.Id);
                }
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Falló la consulta de destinatarios para {ActivityId}", activity.Id);
        }
    }
}
