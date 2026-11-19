using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Application.Services.Activities;

/// <summary>
/// PUBLISHED → IN_PROGRESS al llegar su horario (SCRUM-50). Idempotente: una actividad ya iniciada
/// no vuelve a cambiar, y la concurrencia entre nodos la resuelve xmin. Se audita como acción del sistema.
/// </summary>
public sealed class ActivityStarter(IActivityStore activities, AuditRecorder audit, IUnitOfWork unitOfWork)
{
    public async Task<int> StartDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var due = await activities.ListDueToStartAsync(nowUtc, 200, cancellationToken);
        var started = 0;
        foreach (var activity in due)
        {
            if (activity.StartIfDue(nowUtc))
            {
                started++;
                await audit.RecordSystemAsync(
                    AuditActions.ActivityStarted,
                    AuditResourceTypes.Activity,
                    activity.Id,
                    new Dictionary<string, object?> { ["scheduledStart"] = activity.StartsAt },
                    nowUtc,
                    cancellationToken);
            }
        }

        if (started > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return started;
    }
}
