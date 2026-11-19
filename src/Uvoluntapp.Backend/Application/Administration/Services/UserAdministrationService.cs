using Uvoluntapp.Backend.Application.Accounts.Suspension;
using Uvoluntapp.Backend.Application.Administration.Contracts;
using Uvoluntapp.Backend.Application.Administration.Ports;
using Uvoluntapp.Backend.Application.Audit;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Services;

/// <summary>Consulta de usuarios y bajas o reactivaciones de cuentas (SCRUM-61).</summary>
public sealed class UserAdministrationService(
    AdminAccessGuard guard,
    IAccountDirectory accounts,
    IRoleAssignmentStore roles,
    AccountSuspensionService suspension,
    AuditRecorder audit,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedItems<UserSummaryResponse>> ListAsync(
        Guid actorId,
        AccountStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var result = await accounts.SearchAsync(status, search, page, pageSize, cancellationToken);
        return new PaginatedItems<UserSummaryResponse>(
            result.Items.Select(u => new UserSummaryResponse(
                u.Id, u.Email, u.FirstName, u.LastName, u.Status.ToString(), u.CreatedAt)).ToList(),
            new(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<UserDetailResponse> GetAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var user = await accounts.FindAsync(userId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe.");
        var assignments = await roles.ListForUserAsync(userId, cancellationToken);

        return new UserDetailResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.InstitutionalEmail,
            user.InstitutionalEmailVerifiedAt,
            user.Status.ToString(),
            assignments.Select(RoleAssignmentResponse.From).ToList(),
            user.CreatedAt,
            user.SuspendedAt);
    }

    public async Task SuspendAsync(
        Guid actorId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var actorRoles = await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var result = await suspension.SuspendAsync(userId, reason, actorId, actorRoles, null, cancellationToken);

        await accounts.SetStatusAsync(userId, result.Status, result.SuspendedAt, cancellationToken);
        await accounts.RevokeSessionsAsync(result.SessionIdsToRevoke, result.SuspendedAt, cancellationToken);
        await audit.RecordAccountStatusChangedAsync(
            actorId, userId, AccountStatus.ACTIVE, result.Status, result.Reason, result.SuspendedAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReactivateAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var actorRoles = await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var result = await suspension.ReactivateAsync(userId, actorId, actorRoles, null, cancellationToken);

        await accounts.SetStatusAsync(userId, result.Status, null, cancellationToken);
        await audit.RecordAccountStatusChangedAsync(
            actorId, userId, AccountStatus.SUSPENDED, result.Status, null, result.ReactivatedAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
