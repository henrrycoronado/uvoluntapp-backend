using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Policies;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Application.Services.Administration;

/// <summary>
/// Asignación de roles por contexto (SCRUM-57, SCRUM-58).
/// ADMIN: asigna y revoca cualquier rol salvo crear ADMIN (eso exige doble aprobación).
/// PROGRAM_LEADER: asigna, revoca y consulta líderes operativos solo en los programas que lidera.
/// </summary>
public sealed class RoleAssignmentService(
    AdminAccessGuard guard,
    IRoleAssignmentStore roles,
    IAccountDirectory accounts,
    AuditRecorder audit,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedItems<RoleAssignmentResponse>> ListAsync(
        Guid actorId,
        AssignedRole? role,
        RoleScope? scope,
        Guid? userId,
        bool? active,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireActiveAsync(actorId, cancellationToken);
        IReadOnlyCollection<Guid>? scopeIds = null;
        if (!actor.IsGlobalAdmin)
        {
            scopeIds = actor.LedProgramIds;
            if (scopeIds.Count == 0)
            {
                throw new ForbiddenException("Solo un ADMIN o un líder de programa con programas asignados puede consultar asignaciones.");
            }
        }

        var result = await roles.SearchAsync(
            new RoleAssignmentFilter(userId, role, scope, active, actor.AtUtc, page, pageSize, scopeIds),
            cancellationToken);

        return new PaginatedItems<RoleAssignmentResponse>(
            result.Items.Select(RoleAssignmentResponse.From).ToList(),
            new PaginationMeta(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<RoleAssignmentResponse> GetAsync(
        Guid actorId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireActiveAsync(actorId, cancellationToken);
        var assignment = await roles.FindAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("La asignación no existe.");

        var visible = actor.IsGlobalAdmin
            || assignment.UserId == actorId
            || (assignment.ScopeId is { } programId && actor.LedProgramIds.Contains(programId));
        return visible
            ? RoleAssignmentResponse.From(assignment)
            : throw new ForbiddenException("No puedes consultar esta asignación.");
    }

    public async Task<RoleAssignmentResponse> CreateAsync(
        Guid actorId,
        CreateRoleAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireActiveAsync(actorId, cancellationToken);

        var userId = Parse.ParseGuid(request.UserId, nameof(request.UserId));
        var role = Parse.ParseEnum<AssignedRole>(request.Role, nameof(request.Role));
        var scope = Parse.ParseEnum<RoleScope>(request.Scope, nameof(request.Scope));
        var scopeId = string.IsNullOrWhiteSpace(request.ScopeId)
            ? (Guid?)null
            : Parse.ParseGuid(request.ScopeId, nameof(request.ScopeId));

        if (role == AssignedRole.ADMIN)
        {
            throw new DomainRuleViolationException(
                "Un nuevo ADMIN requiere una solicitud de promoción con doble aprobación.");
        }

        if (!actor.IsGlobalAdmin && !LeaderAssignmentPolicy.CanAssign(actor.Assignments, role, scope, scopeId, actor.AtUtc))
        {
            throw new ForbiddenException("Solo puedes asignar líderes operativos en los programas que lideras.");
        }

        if (userId == actorId && !actor.IsGlobalAdmin)
        {
            throw new DomainRuleViolationException("No puedes asignarte roles a ti mismo.");
        }

        var status = await accounts.FindStatusAsync(userId, cancellationToken)
            ?? throw new NotFoundException("La cuenta no existe.");
        if (status != AccountStatus.ACTIVE)
        {
            throw new DomainRuleViolationException("Solo se asignan roles a cuentas ACTIVE.");
        }

        var existing = await roles.ListForUserAsync(userId, cancellationToken);
        if (existing.Any(a => a.IsActiveAt(actor.AtUtc) && a.Role == role && a.Scope == scope && a.ScopeId == scopeId))
        {
            throw new ConflictException("La persona ya tiene esa asignación vigente.");
        }

        var assignment = RoleAssignment.Assign(
            userId,
            role,
            scope,
            scopeId,
            actorId,
            request.ValidFrom,
            request.ValidUntil);

        await roles.AddAsync(assignment, cancellationToken);
        await audit.RecordRoleAssignedAsync(actorId, assignment, actor.AtUtc, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RoleAssignmentResponse.From(assignment);
    }

    public async Task<RoleAssignmentResponse> RevokeAsync(
        Guid actorId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequireActiveAsync(actorId, cancellationToken);
        var assignment = await roles.FindAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("La asignación no existe.");

        if (!actor.IsGlobalAdmin && !LeaderAssignmentPolicy.CanRevoke(actor.Assignments, assignment, actor.AtUtc))
        {
            throw new ForbiddenException("Solo puedes revocar líderes operativos de los programas que lideras.");
        }

        if (assignment.RevokedAt is not null)
        {
            throw new ConflictException("La asignación ya está revocada.");
        }

        if (assignment.Role == AssignedRole.ADMIN && assignment.IsActiveAt(actor.AtUtc))
        {
            var activeAdmins = await roles.CountActiveAdminAssignmentsAsync(actor.AtUtc, cancellationToken);
            AdminGrantPolicy.EnsureCanRemoveAdmin(activeAdmins);
        }

        assignment.Revoke(actor.AtUtc);
        await audit.RecordRoleRevokedAsync(actorId, assignment, actor.AtUtc, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RoleAssignmentResponse.From(assignment);
    }

    private async Task<ActorAccess> RequireActiveAsync(Guid actorId, CancellationToken cancellationToken)
    {
        var actor = await guard.ResolveAsync(actorId, cancellationToken);
        return actor.IsActive ? actor : throw new ForbiddenException("Tu cuenta no está activa.");
    }
}
