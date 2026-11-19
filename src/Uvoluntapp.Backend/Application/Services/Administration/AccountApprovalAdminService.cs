using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Administration;

/// <summary>Aprobación o rechazo de cuentas externas por un ADMIN (SCRUM-108).</summary>
public sealed class AccountApprovalAdminService(
    AdminAccessGuard guard,
    IAccountApprovalStore approvals,
    IAccountDirectory accounts,
    AuditRecorder audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<PaginatedItems<ApprovalRequestSummary>> ListAsync(
        Guid actorId,
        AccountApprovalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var result = await approvals.SearchAsync(status, page, pageSize, cancellationToken);
        var items = result.Items.Select(view => ToSummary(view.Request, view.Email)).ToList();
        return new PaginatedItems<ApprovalRequestSummary>(
            items,
            new(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task ApproveAsync(Guid actorId, Guid requestId, CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var request = await RequirePendingAsync(requestId, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;

        request.Approve(actorId, now);
        await accounts.SetStatusAsync(request.UserId, AccountStatus.ACTIVE, null, cancellationToken);

        // Aprobar una cuenta nueva es también verificarla.
        await accounts.SetVerificationAsync(request.UserId, now, actorId, cancellationToken);
        await audit.RecordAccountApprovedAsync(actorId, request, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(
        Guid actorId,
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var request = await RequirePendingAsync(requestId, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;

        request.Reject(actorId, reason, now);
        await accounts.SetStatusAsync(request.UserId, AccountStatus.REJECTED, null, cancellationToken);
        await audit.RecordAccountRejectedAsync(actorId, request, request.RejectionReason ?? reason, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static ApprovalRequestSummary ToSummary(AccountApprovalRequest request, string email) => new(
        request.Id,
        request.UserId,
        email,
        request.FirstName,
        request.LastName,
        request.Phone,
        request.Relationship,
        request.Status.ToString(),
        request.RejectionReason,
        request.ReviewedBy,
        request.CreatedAt,
        request.ReviewedAt);

    private async Task<AccountApprovalRequest> RequirePendingAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await approvals.FindAsync(requestId, cancellationToken)
            ?? throw new NotFoundException("La solicitud no existe.");
        if (request.Status != AccountApprovalStatus.PENDING_APPROVAL)
        {
            throw new ConflictException("La solicitud ya fue resuelta.");
        }

        return request;
    }
}
