using Uvoluntapp.Backend.Application.DTOs.Programs;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Application.Services.Audit;

namespace Uvoluntapp.Backend.Application.Services.Programs;

public sealed class PeriodAdministration(PlanningAccess access, IAcademicPeriodStore periods, AuditRecorder audit, TimeProvider clock)
{
    public async Task<AcademicPeriodResponse> ChangeAsync(Guid actorId, Guid id, string label, DateOnly? endDate, bool close, CancellationToken ct)
    {
        var actor = await access.ResolveAsync(actorId, ct);
        access.RequireAdmin(actor, "Solo ADMIN administra periodos.");
        var today = DateTime.SpecifyKind(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(-4)).Date, DateTimeKind.Utc);
        var period = await periods.ChangeAsync(
            id,
            label,
            endDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            today,
            close,
            p => audit.RecordAsync(actorId, close ? "PERIOD_CLOSED" : "PERIOD_UPDATED", "AcademicPeriod", id, new Dictionary<string, object?> { ["label"] = p.Label, ["resultPeriodId"] = p.Id }, cancellationToken: ct),
            ct);
        return AcademicPeriodResponse.From(period);
    }
}
