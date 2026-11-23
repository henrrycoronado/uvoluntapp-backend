using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.Exceptions.Activities;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Application.Services.Programs;
using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.Services.Activities;

public sealed class EnrollmentService(PlanningAccess access, IActivityStore activities, IEnrollmentStore store, AuditRecorder audit, TimeProvider clock)
{
    public Task<EnrollmentResponse> EnrollAsync(Guid userId, Guid activityId, Guid? groupId, CancellationToken ct) =>
        store.WithLocksAsync(
            userId,
            activityId,
            async () =>
        {
            await access.ResolveAsync(userId, ct);
            var activity = await FindAsync(activityId, ct);
            if (!activity.IsPublic)
            {
                throw new NotFoundException("La actividad no existe.");
            }

            EnsureOpen(activity);
            var group = activity.Groups.Find(g => g.Id == groupId);
            if ((groupId.HasValue && group == null) || (!groupId.HasValue && activity.Groups.Count != 0))
            {
                throw new EnrollmentException("INVALID_GROUP", "Selecciona un grupo de esta actividad.");
            }

            var existing = await store.ListAsync(userId, activityId, ct);
            if (existing.Any(e => e.CancelledAt == null && (e.GroupId == groupId || !activity.AllowMultipleGroups)))
            {
                throw new EnrollmentException("ALREADY_ENROLLED", "Ya tienes inscripción en este destino.");
            }

            var capacity = group == null ? activity.Capacity : group.Capacity;
            if (capacity.HasValue && await store.OccupancyAsync(activityId, groupId, ct) >= capacity.Value)
            {
                throw new EnrollmentException("CAPACITY_FULL", "El cupo está agotado.");
            }

            if (await store.OverlapsAsync(userId, group?.StartsAt ?? activity.StartsAt, group?.EndsAt ?? activity.EndsAt, ct))
            {
                throw new EnrollmentException("TIME_OVERLAP", "El horario coincide con otra inscripción.");
            }

            var enrollment = new Enrollment { ActivityId = activityId, UserId = userId, GroupId = groupId };
            store.Add(enrollment);
            await audit.RecordAsync(userId, "ENROLLMENT_CREATED", "Enrollment", enrollment.Id, new Dictionary<string, object?> { ["activityId"] = activityId }, cancellationToken: ct);
            return EnrollmentResponse.From(enrollment);
        },
            ct);

    public Task<EnrollmentResponse> CancelAsync(Guid userId, Guid activityId, Guid enrollmentId, CancellationToken ct) =>
        store.WithLocksAsync(
            userId,
            activityId,
            async () =>
        {
            await access.ResolveAsync(userId, ct);
            var enrollment = (await store.ListAsync(userId, activityId, ct)).Find(e => e.Id == enrollmentId) ?? throw new NotFoundException("La inscripción no existe.");
            if (enrollment.CancelledAt == null)
            {
                EnsureOpen(await FindAsync(activityId, ct));
                enrollment.Cancel(clock.GetUtcNow().UtcDateTime);
                await audit.RecordAsync(userId, "ENROLLMENT_CANCELLED", "Enrollment", enrollment.Id, new Dictionary<string, object?> { ["activityId"] = activityId }, cancellationToken: ct);
            }

            return EnrollmentResponse.From(enrollment);
        },
            ct);

    public async Task<List<EnrollmentResponse>> ListAsync(Guid userId, Guid? activityId, CancellationToken ct)
    {
        var actor = await access.ResolveAsync(userId, ct);
        if (activityId.HasValue)
        {
            access.RequireActivityManager(actor, await FindAsync(activityId.Value, ct));
        }

        var enrollments = await store.ListAsync(activityId.HasValue ? null : userId, activityId, ct);
        var rows = enrollments.Select(EnrollmentResponse.From).ToList();
        if (!activityId.HasValue || rows.Count == 0)
        {
            return rows;
        }

        // La lista de una actividad es para su responsable: añade nombre y correo para identificar a cada inscrito.
        var people = await store.ParticipantsAsync(rows.Select(r => r.UserId).Distinct().ToList(), ct);
        return rows.Select(r => people.TryGetValue(r.UserId, out var p) ? r.WithParticipant(p.Name, p.Email) : r).ToList();
    }

    private async Task<Activity> FindAsync(Guid id, CancellationToken ct) =>
        await activities.FindAsync(id, ct) ?? throw new NotFoundException("La actividad no existe.");

    private void EnsureOpen(Activity activity)
    {
        if (!activity.AcceptsEnrollment(clock.GetUtcNow().UtcDateTime))
        {
            throw new EnrollmentException("STATE_NOT_OPEN", "La actividad no admite inscripciones en este momento.");
        }
    }
}
