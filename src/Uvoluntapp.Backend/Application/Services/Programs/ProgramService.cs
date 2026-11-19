using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.DTOs.Programs;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Entities.Programs;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Application.Services.Programs;

/// <summary>Programas (SCRUM-45, SCRUM-46, SCRUM-103) y su equipo de líderes (SCRUM-122).</summary>
public sealed class ProgramService(
    PlanningAccess access,
    IProgramStore programs,
    IAcademicPeriodStore periods,
    IActivityStore activities,
    IRoleAssignmentStore roles,
    IAccountDirectory accounts,
    AuditRecorder audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    private static readonly ProgramStatus[] _publicStatuses = [ProgramStatus.ACTIVE];

    public async Task<PaginatedItems<ProgramResponse>> ListAsync(
        Guid actorId,
        ProgramStatus? status,
        bool includeArchived,
        Guid? periodId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);

        // Líderes y ADMIN ven todos los programas; el voluntario, solo los activos. Archivados: solo ADMIN.
        IReadOnlyCollection<ProgramStatus>? statuses = PlanningAccess.CanSeeAllPrograms(actor)
            ? status is { } value ? [value] : null
            : _publicStatuses;
        var result = await programs.SearchAsync(statuses, includeArchived && actor.IsGlobalAdmin, periodId, page, pageSize, cancellationToken);
        return new PaginatedItems<ProgramResponse>(
            result.Items.Select(p => ProgramResponse.From(p, !p.IsArchived && access.CanManageProgram(actor, p.Id))).ToList(),
            new PaginationMeta(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<ProgramResponse> GetAsync(Guid actorId, Guid programId, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        var program = await VisibleAsync(actor, programId, cancellationToken);
        return ProgramResponse.From(program, !program.IsArchived && access.CanManageProgram(actor, program.Id));
    }

    public async Task<ProgramResponse> CreateAsync(Guid actorId, ProgramDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        access.RequireAdmin(actor, "Solo un ADMIN crea programas.");

        var now = Now();
        var program = VolunteerProgram.Create(request.Name, request.Description, request.Contact, request.ReferenceSchedule, now);
        await programs.AddAsync(program, cancellationToken);
        await audit.RecordAsync(actorId, AuditActions.ProgramCreated, AuditResourceTypes.Program, program.Id, new Dictionary<string, object?> { ["name"] = program.Name }, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProgramResponse.From(program, canManage: true);
    }

    public async Task<ProgramResponse> UpdateAsync(Guid actorId, Guid programId, ProgramDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var (actor, program) = await ManagedAsync(actorId, programId, cancellationToken);
        var now = Now();
        program.UpdateDetails(request.Name, request.Description, request.Contact, request.ReferenceSchedule, now);
        await audit.RecordAsync(actorId, AuditActions.ProgramUpdated, AuditResourceTypes.Program, program.Id, null, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProgramResponse.From(program, access.CanManageProgram(actor, program.Id));
    }

    public async Task<ProgramResponse> ActivateAsync(Guid actorId, Guid programId, CancellationToken cancellationToken = default)
    {
        var (actor, program) = await ManagedAsync(actorId, programId, cancellationToken);
        var now = Now();
        var previous = program.Status;
        program.Activate(now);
        await RecordStatusAsync(actorId, program, AuditActions.ProgramActivated, previous, null, now, cancellationToken);
        return ProgramResponse.From(program, access.CanManageProgram(actor, program.Id));
    }

    public async Task<ProgramResponse> DeactivateAsync(Guid actorId, Guid programId, CancellationToken cancellationToken = default)
    {
        var (actor, program) = await ManagedAsync(actorId, programId, cancellationToken);
        await EnsureNoLiveActivitiesAsync(program.Id, "desactivar", cancellationToken);
        var now = Now();
        var previous = program.Status;
        program.Deactivate(now);
        await RecordStatusAsync(actorId, program, AuditActions.ProgramDeactivated, previous, null, now, cancellationToken);
        return ProgramResponse.From(program, access.CanManageProgram(actor, program.Id));
    }

    public async Task ArchiveAsync(Guid actorId, Guid programId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("El motivo para archivar es obligatorio.");
        }

        var (_, program) = await ManagedAsync(actorId, programId, cancellationToken);
        await EnsureNoLiveActivitiesAsync(program.Id, "archivar", cancellationToken);
        var now = Now();
        program.Archive(now);
        await RecordStatusAsync(actorId, program, AuditActions.ProgramArchived, program.Status, reason.Trim(), now, cancellationToken);
    }

    public async Task<IReadOnlyList<AcademicPeriodResponse>> ListPeriodsAsync(Guid actorId, Guid programId, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        await VisibleAsync(actor, programId, cancellationToken);
        return (await periods.ListForProgramAsync(programId, cancellationToken)).Select(AcademicPeriodResponse.From).ToList();
    }

    /// <summary>Equipo del programa: líderes y operativos asignados y sus delegaciones a actividades (SCRUM-122).</summary>
    /// <param name="actorId">Quien consulta: ADMIN o cualquier líder.</param>
    /// <param name="programId">Programa.</param>
    /// <param name="cancellationToken">Cancelación.</param>
    /// <returns>Asignaciones vigentes con el nombre de cada persona.</returns>
    public async Task<IReadOnlyList<ProgramTeamMember>> TeamAsync(Guid actorId, Guid programId, CancellationToken cancellationToken = default)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        if (!PlanningAccess.CanSeeAllPrograms(actor))
        {
            throw new ForbiddenException("El equipo de un programa lo consultan ADMIN y líderes.");
        }

        await VisibleAsync(actor, programId, cancellationToken);
        var activityIds = await activities.ListIdsForProgramAsync(programId, cancellationToken);
        var assignments = await roles.ListActiveForScopesAsync([programId], activityIds, actor.AtUtc, cancellationToken);
        var titles = new Dictionary<Guid, string>();
        foreach (var id in assignments.Where(a => a.Scope == RoleScope.ACTIVITY).Select(a => a.ScopeId!.Value).Distinct())
        {
            titles[id] = (await activities.FindAsync(id, cancellationToken))?.Title ?? "Actividad";
        }

        var members = new List<ProgramTeamMember>();
        foreach (var assignment in assignments.OrderBy(a => a.Role).ThenBy(a => a.ValidFrom ?? a.CreatedAt))
        {
            var user = await accounts.FindAsync(assignment.UserId, cancellationToken);
            members.Add(new ProgramTeamMember(
                assignment.Id,
                assignment.UserId,
                user is null ? "Persona" : $"{user.FirstName} {user.LastName}".Trim(),
                user?.Email ?? string.Empty,
                assignment.Role.ToString(),
                assignment.Scope.ToString(),
                assignment.ScopeId!.Value,
                assignment.Scope == RoleScope.ACTIVITY ? titles[assignment.ScopeId!.Value] : null,
                assignment.ValidFrom,
                assignment.ValidUntil,
                assignment.GrantedBy));
        }

        return members;
    }

    private async Task<VolunteerProgram> VisibleAsync(ActorAccess actor, Guid programId, CancellationToken cancellationToken)
    {
        var program = await programs.FindAsync(programId, cancellationToken);
        var visible = program is not null
            && (actor.IsGlobalAdmin
                || (!program.IsArchived && (PlanningAccess.CanSeeAllPrograms(actor) || program.Status == ProgramStatus.ACTIVE)));
        return visible ? program! : throw new NotFoundException("El programa no existe.");
    }

    private async Task<(ActorAccess Actor, VolunteerProgram Program)> ManagedAsync(Guid actorId, Guid programId, CancellationToken cancellationToken)
    {
        var actor = await access.ResolveAsync(actorId, cancellationToken);
        var program = await VisibleAsync(actor, programId, cancellationToken);
        access.RequireProgramManager(actor, program.Id);
        return (actor, program);
    }

    private async Task EnsureNoLiveActivitiesAsync(Guid programId, string verb, CancellationToken cancellationToken)
    {
        if (await programs.HasLiveActivitiesAsync(programId, cancellationToken))
        {
            throw new ConflictException($"No se puede {verb} un programa con actividades publicadas o en ejecución.");
        }
    }

    private async Task RecordStatusAsync(
        Guid actorId,
        VolunteerProgram program,
        string action,
        ProgramStatus previous,
        string? reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        await audit.RecordAsync(
            actorId,
            action,
            AuditResourceTypes.Program,
            program.Id,
            new Dictionary<string, object?>
            {
                ["previousStatus"] = previous.ToString(),
                ["newStatus"] = program.IsArchived ? "ARCHIVED" : program.Status.ToString(),
                ["reason"] = reason,
            },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private DateTime Now() => clock.GetUtcNow().UtcDateTime;
}
