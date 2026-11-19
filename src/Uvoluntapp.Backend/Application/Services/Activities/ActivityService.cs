using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Application.Services.Programs;
using Uvoluntapp.Backend.Domain.Entities.Activities;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Application.Services.Activities;

/// <summary>Actividades: planificación, configuración y ciclo de vida (SCRUM-47 a 51, 53, 54, 123).</summary>
public sealed class ActivityService(
    PlanningAccess access,
    IActivityStore activities,
    IProgramStore programs,
    IAcademicPeriodStore periods,
    IRoleAssignmentStore roles,
    AuditRecorder audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    private static readonly ActivityStatus[] _publicStatuses = [ActivityStatus.PUBLISHED, ActivityStatus.IN_PROGRESS];

    public async Task<PaginatedItems<ActivityResponse>> ListAsync(
        Guid actorId,
        ActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        var reviewer = PlanningAccess.CanSeeAllPrograms(actor);

        // Voluntario: solo lo público. Líderes: todo lo no borrador y los borradores de su ámbito. ADMIN: todo.
        IReadOnlyCollection<ActivityStatus>? statuses = reviewer
            ? query.Status is { } value ? [value] : null
            : query.Status is { } wanted && _publicStatuses.Contains(wanted) ? [wanted] : _publicStatuses;
        var result = await activities.SearchAsync(
            new ActivitySearch(
                query.ProgramId,
                query.PeriodId,
                statuses,
                query.Independent,
                query.GroupLabel,
                query.From,
                query.To,
                AllDrafts: actor.IsGlobalAdmin,
                DraftProgramIds: PlanningAccess.InternalProgramIds(actor),
                DraftActivityIds: PlanningAccess.InternalActivityIds(actor),
                OnlyActivePrograms: !reviewer,
                query.Page,
                query.PageSize),
            cancellationToken);
        var names = await programs.NamesAsync(result.Items.Where(a => a.ProgramId is not null).Select(a => a.ProgramId!.Value).Distinct().ToList(), cancellationToken);
        return new PaginatedItems<ActivityResponse>(
            result.Items.Select(a => Response(actor, a, names)).ToList(),
            new PaginationMeta(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<ActivityResponse> GetAsync(Guid actorId, Guid activityId, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        var activity = await VisibleAsync(actor, activityId, cancellationToken);
        return await ResponseAsync(actor, activity, cancellationToken);
    }

    public async Task<ActivityResponse> CreateAsync(Guid actorId, ActivityRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        if (request.ProgramId is { } programId)
        {
            var program = await programs.FindAsync(programId, cancellationToken);
            if (program is null || program.IsArchived)
            {
                throw new NotFoundException("El programa no existe.");
            }

            access.RequireProgramManager(actor, programId);
        }
        else
        {
            access.RequireAdmin(actor, "Solo un ADMIN crea actividades libres; luego puede asignar su gestión a un líder.");
        }

        var periodId = request.PeriodId ?? throw new DomainRuleViolationException("El periodo académico es obligatorio.");
        var period = await periods.FindAsync(periodId, cancellationToken) ?? throw new DomainRuleViolationException("El periodo académico no existe.");
        var plan = Plan(request);
        EnsureWithinPeriod(period, plan.StartsAt);

        var now = Now();
        var activity = Activity.Create(request.ProgramId, period.Id, plan, now);
        if (activity.ProgramId is { } linkedProgram)
        {
            await periods.EnsureProgramLinkAsync(linkedProgram, period.Id, cancellationToken);
        }

        await activities.AddAsync(activity, cancellationToken);
        await audit.RecordAsync(actorId, AuditActions.ActivityCreated, AuditResourceTypes.Activity, activity.Id, new Dictionary<string, object?> { ["programId"] = activity.ProgramId, ["title"] = activity.Title }, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ResponseAsync(actor, activity, cancellationToken);
    }

    public async Task<ActivityResponse> UpdateAsync(Guid actorId, Guid activityId, ActivityRequest request, CancellationToken cancellationToken = default)
    {
        var (actor, activity) = await ManagedAsync(actorId, activityId, cancellationToken);
        var now = Now();
        var changed = activity.UpdatePlan(Plan(request), now);
        if (changed.Count > 0)
        {
            await audit.RecordAsync(actorId, AuditActions.ActivityUpdated, AuditResourceTypes.Activity, activity.Id, new Dictionary<string, object?> { ["fields"] = changed }, now, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return await ResponseAsync(actor, activity, cancellationToken);
    }

    public async Task<ActivityResponse> PublishAsync(Guid actorId, Guid activityId, CancellationToken cancellationToken = default)
    {
        var (actor, activity) = await ManagedAsync(actorId, activityId, cancellationToken);
        if (activity.ProgramId is { } programId)
        {
            var program = await programs.FindAsync(programId, cancellationToken);
            if (program is null || program.IsArchived || program.Status != ProgramStatus.ACTIVE)
            {
                throw new DomainRuleViolationException("Solo se publican actividades de un programa activo.");
            }
        }

        var now = Now();
        activity.Publish(now);
        return await RecordAsync(actor, activity, AuditActions.ActivityPublished, null, now, cancellationToken);
    }

    public async Task<ActivityResponse> FinishAsync(Guid actorId, Guid activityId, CancellationToken cancellationToken = default)
    {
        var (actor, activity) = await ManagedAsync(actorId, activityId, cancellationToken);
        var now = Now();
        activity.Finish(now);
        return await RecordAsync(actor, activity, AuditActions.ActivityFinished, null, now, cancellationToken);
    }

    public async Task<ActivityResponse> CancelAsync(Guid actorId, Guid activityId, string reason, CancellationToken cancellationToken = default)
    {
        var (actor, activity) = await ManagedAsync(actorId, activityId, cancellationToken);
        var now = Now();
        activity.Cancel(reason, now);
        return await RecordAsync(actor, activity, AuditActions.ActivityCancelled, new Dictionary<string, object?> { ["reason"] = activity.CancellationReason }, now, cancellationToken);
    }

    public async Task DiscardAsync(Guid actorId, Guid activityId, CancellationToken cancellationToken = default)
    {
        var (actor, activity) = await ManagedAsync(actorId, activityId, cancellationToken);
        var now = Now();
        activity.Discard(now);
        await RecordAsync(actor, activity, AuditActions.ActivityDiscarded, null, now, cancellationToken);
    }

    public async Task<RescheduleActivityResponse> RescheduleAsync(Guid actorId, Guid activityId, RescheduleActivityRequest request, CancellationToken cancellationToken = default)
    {
        var (actor, activity) = await ManagedAsync(actorId, activityId, cancellationToken);
        var now = Now();
        var record = activity.Reschedule(Utc(request.NewStartsAt), Utc(request.NewEndsAt), request.Reason, actorId, now);
        await activities.AddRescheduleAsync(record, cancellationToken);
        var conflicts = await ConflictsAsync(activity.Id, activity.ProgramId, activity.StartsAt, activity.EndsAt, activity.Location, cancellationToken);
        await audit.RecordAsync(
            actorId,
            AuditActions.ActivityRescheduled,
            AuditResourceTypes.Activity,
            activity.Id,
            new Dictionary<string, object?>
            {
                ["reason"] = record.Reason,
                ["previousStartsAt"] = record.PreviousStartsAt,
                ["newStartsAt"] = record.NewStartsAt,
                ["conflicts"] = conflicts.Count,
            },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RescheduleActivityResponse(await ResponseAsync(actor, activity, cancellationToken), ActivityRescheduleResponse.From(record), conflicts);
    }

    public async Task<IReadOnlyList<ActivityRescheduleResponse>> ListReschedulesAsync(Guid actorId, Guid activityId, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        var activity = await VisibleAsync(actor, activityId, cancellationToken);
        if (!access.CanViewActivityInternals(actor, activity))
        {
            throw new ForbiddenException("El historial de reprogramaciones lo ven los responsables de la actividad.");
        }

        return (await activities.ListReschedulesAsync(activity.Id, cancellationToken)).Select(ActivityRescheduleResponse.From).ToList();
    }

    public async Task<CheckConflictsResponse> CheckConflictsAsync(Guid actorId, CheckConflictsRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        var startsAt = Utc(request.StartsAt);
        var endsAt = Utc(request.EndsAt);
        if (endsAt <= startsAt)
        {
            throw new DomainRuleViolationException("El fin debe ser posterior al inicio.");
        }

        Guid? programId = request.ProgramId;
        if (request.ActivityId is { } activityId)
        {
            var activity = await VisibleAsync(actor, activityId, cancellationToken);
            access.RequireActivityManager(actor, activity);
            programId = activity.ProgramId;
        }
        else if (programId is { } id)
        {
            access.RequireProgramManager(actor, id);
        }
        else
        {
            access.RequireAdmin(actor, "Solo un ADMIN planifica actividades libres.");
        }

        var conflicts = await ConflictsAsync(request.ActivityId, programId, startsAt, endsAt, request.Location, cancellationToken);
        return new CheckConflictsResponse(conflicts.Count > 0, conflicts);
    }

    /// <summary>
    /// Cambiar el programa de una actividad o volverla libre (SCRUM-123). Solo ADMIN, en cualquier estado.
    /// Las asignaciones sobre la actividad dejan de cubrirla: sus responsables pasan a ser los del nuevo destino.
    /// </summary>
    /// <param name="actorId">ADMIN que hace el cambio.</param>
    /// <param name="activityId">Actividad.</param>
    /// <param name="request">Programa destino (vacío = libre) y motivo.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>La actividad actualizada.</returns>
    public async Task<ActivityResponse> MoveAsync(Guid actorId, Guid activityId, MoveActivityRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        access.RequireAdmin(actor, "Solo un ADMIN cambia el programa de una actividad.");
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new DomainRuleViolationException("El motivo es obligatorio.");
        }

        var activity = await activities.FindAsync(activityId, cancellationToken) ?? throw new NotFoundException("La actividad no existe.");
        if (request.ProgramId is { } programId)
        {
            var program = await programs.FindAsync(programId, cancellationToken);
            if (program is null || program.IsArchived)
            {
                throw new NotFoundException("El programa destino no existe.");
            }

            await periods.EnsureProgramLinkAsync(programId, activity.AcademicPeriodId, cancellationToken);
        }

        var now = Now();
        var previous = activity.ProgramId;
        activity.MoveTo(request.ProgramId, now);
        var delegated = await roles.ListActiveForScopesAsync([], [activity.Id], now, cancellationToken);
        foreach (var assignment in delegated)
        {
            assignment.Revoke(now);
            await audit.RecordRoleRevokedAsync(actorId, assignment, now, cancellationToken);
        }

        await audit.RecordAsync(
            actorId,
            AuditActions.ActivityMoved,
            AuditResourceTypes.Activity,
            activity.Id,
            new Dictionary<string, object?>
            {
                ["previousProgramId"] = previous,
                ["newProgramId"] = activity.ProgramId,
                ["reason"] = request.Reason.Trim(),
                ["revokedAssignments"] = delegated.Count,
            },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ResponseAsync(actor, activity, cancellationToken);
    }

    private static DateTime Utc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static ActivityPlan Plan(ActivityRequest request)
    {
        if (!Enum.TryParse<ActivityModality>(request.Modality, ignoreCase: false, out var modality) || !Enum.IsDefined(modality))
        {
            throw new DomainRuleViolationException("La modalidad debe ser IN_PERSON, VIRTUAL o HYBRID.");
        }

        return new ActivityPlan(
            request.Title,
            request.Description,
            modality,
            request.Location,
            request.MeetingUrl,
            Utc(request.StartsAt),
            Utc(request.EndsAt),
            request.RequiresRegistration,
            request.Capacity,
            request.ExpectedHours,
            request.ParticipationConditions,
            request.PeriodicityNote,
            request.GroupLabel);
    }

    // Periodo incoherente (SCRUM-47): la actividad empieza dentro del periodo.
    private static void EnsureWithinPeriod(AcademicPeriod period, DateTime startsAt)
    {
        if (startsAt < period.StartsAt || startsAt >= period.EndsAt.AddDays(1))
        {
            throw new DomainRuleViolationException($"La actividad debe empezar dentro del periodo {period.Label}.");
        }
    }

    private static string Describe(Activity activity) => $"{activity.StartsAt:yyyy-MM-dd HH:mm} a {activity.EndsAt:HH:mm} (UTC)";

    /// <summary>
    /// Advertencias de solapamiento (SCRUM-54): mismo responsable (líder con asignación vigente que cubre ambas)
    /// o mismo lugar, contra actividades publicadas o en ejecución. No identifica a las personas.
    /// </summary>
    private async Task<IReadOnlyList<ConflictWarning>> ConflictsAsync(
        Guid? activityId,
        Guid? programId,
        DateTime startsAt,
        DateTime endsAt,
        string? location,
        CancellationToken cancellationToken)
    {
        var overlapping = await activities.ListOverlappingAsync(startsAt, endsAt, activityId, cancellationToken);
        if (overlapping.Count == 0)
        {
            return [];
        }

        var now = Now();
        var programIds = overlapping.Where(a => a.ProgramId is not null).Select(a => a.ProgramId!.Value).Append(programId ?? Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
        var activityIds = overlapping.Select(a => a.Id).Append(activityId ?? Guid.Empty).Where(id => id != Guid.Empty).Distinct().ToList();
        var assignments = await roles.ListActiveForScopesAsync(programIds, activityIds, now, cancellationToken);

        HashSet<Guid> Responsibles(Guid? candidateActivityId, Guid? candidateProgramId) => assignments
            .Where(a => (a.Scope == RoleScope.ACTIVITY && a.ScopeId == candidateActivityId)
                || (a.Scope == RoleScope.PROGRAM && candidateProgramId is not null && a.ScopeId == candidateProgramId))
            .Select(a => a.UserId)
            .ToHashSet();

        var mine = Responsibles(activityId, programId);
        var place = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        var warnings = new List<ConflictWarning>();
        foreach (var other in overlapping)
        {
            if (mine.Count > 0 && mine.Overlaps(Responsibles(other.Id, other.ProgramId)))
            {
                warnings.Add(new ConflictWarning("LEADER_OVERLAP", other.Id, other.Title, $"Un responsable de esta actividad también está a cargo de \"{other.Title}\" el {Describe(other)}."));
            }

            if (place is not null && string.Equals(place, other.Location?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(new ConflictWarning("LOCATION_OVERLAP", other.Id, other.Title, $"El lugar \"{place}\" ya está ocupado por \"{other.Title}\" el {Describe(other)}."));
            }
        }

        return warnings;
    }

    private async Task<Activity> VisibleAsync(ActorAccess actor, Guid activityId, CancellationToken cancellationToken)
    {
        var activity = await activities.FindAsync(activityId, cancellationToken);
        if (activity is null || (activity.DeletedAt is not null && !actor.IsGlobalAdmin))
        {
            throw new NotFoundException("La actividad no existe.");
        }

        // Lo público lo ve cualquiera; los borradores y estados internos, solo quien tiene ámbito sobre la actividad.
        var visible = activity.IsPublic
            || actor.IsGlobalAdmin
            || (PlanningAccess.CanSeeAllPrograms(actor) && activity.Status != ActivityStatus.DRAFT)
            || access.CanViewActivityInternals(actor, activity);
        return visible ? activity : throw new NotFoundException("La actividad no existe.");
    }

    private async Task<(ActorAccess Actor, Activity Activity)> ManagedAsync(Guid actorId, Guid activityId, CancellationToken cancellationToken)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        var activity = await VisibleAsync(actor, activityId, cancellationToken);
        access.RequireActivityManager(actor, activity);
        return (actor, activity);
    }

    private async Task<ActivityResponse> RecordAsync(
        ActorAccess actor,
        Activity activity,
        string action,
        IReadOnlyDictionary<string, object?>? details,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await audit.RecordAsync(actor.ActorId, action, AuditResourceTypes.Activity, activity.Id, details, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ResponseAsync(actor, activity, cancellationToken);
    }

    private async Task<ActivityResponse> ResponseAsync(ActorAccess actor, Activity activity, CancellationToken cancellationToken)
    {
        var names = activity.ProgramId is { } id
            ? await programs.NamesAsync([id], cancellationToken)
            : new Dictionary<Guid, string>();
        return Response(actor, activity, names);
    }

    private ActivityResponse Response(ActorAccess actor, Activity activity, IReadOnlyDictionary<Guid, string> names)
    {
        var internals = access.CanViewActivityInternals(actor, activity) || actor.IsGlobalAdmin;
        var response = ActivityResponse.From(
            activity,
            activity.ProgramId is { } id && names.TryGetValue(id, out var name) ? name : null,
            canManage: activity.DeletedAt is null && access.CanManageActivity(actor, activity),
            canMove: actor.IsGlobalAdmin && activity.DeletedAt is null);

        // El enlace de reunión es dato interno hasta que la actividad es pública.
        return internals || activity.IsPublic ? response : response with { MeetingUrl = null };
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
