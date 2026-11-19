using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Application.Administration.Contracts;
using Uvoluntapp.Backend.Application.Administration.Ports;
using Uvoluntapp.Backend.Application.Audit;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Services;

public sealed class RoleAssignmentService(
    AdminAccessGuard guard,
    IRoleAssignmentStore roles,
    IAccountDirectory accounts,
    AuditRecorder audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
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
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var result = await roles.SearchAsync(
            new RoleAssignmentFilter(userId, role, scope, active, now, page, pageSize),
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
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var assignment = await roles.FindAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("La asignación no existe.");
        return RoleAssignmentResponse.From(assignment);
    }

    public async Task<RoleAssignmentResponse> CreateAsync(
        Guid actorId,
        CreateRoleAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);

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

        var status = await accounts.FindStatusAsync(userId, cancellationToken)
            ?? throw new NotFoundException("La cuenta no existe.");
        if (status != AccountStatus.ACTIVE)
        {
            throw new DomainRuleViolationException("Solo se asignan roles a cuentas ACTIVE.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var existing = await roles.ListForUserAsync(userId, cancellationToken);
        if (existing.Any(a => a.IsActiveAt(now) && a.Role == role && a.Scope == scope && a.ScopeId == scopeId))
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
        await audit.RecordRoleAssignedAsync(actorId, assignment, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RoleAssignmentResponse.From(assignment);
    }

    public async Task<RoleAssignmentResponse> RevokeAsync(
        Guid actorId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var assignment = await roles.FindAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundException("La asignación no existe.");
        var now = clock.GetUtcNow().UtcDateTime;

        if (assignment.RevokedAt is not null)
        {
            throw new ConflictException("La asignación ya está revocada.");
        }

        if (assignment.Role == AssignedRole.ADMIN && assignment.IsActiveAt(now))
        {
            var activeAdmins = await roles.CountActiveAdminAssignmentsAsync(now, cancellationToken);
            AdminGrantPolicy.EnsureCanRemoveAdmin(activeAdmins);
        }

        assignment.Revoke(now);
        await audit.RecordRoleRevokedAsync(actorId, assignment, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return RoleAssignmentResponse.From(assignment);
    }
}
