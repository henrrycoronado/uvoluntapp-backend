using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Policies;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Application.Services.Administration;

/// <summary>Promoción a ADMIN con doble aprobación (SCRUM-107).</summary>
public sealed class AdminGrantService(
    AdminAccessGuard guard,
    IAdminGrantStore grants,
    IRoleAssignmentStore roles,
    IAccountDirectory accounts,
    AuditRecorder audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<PaginatedItems<AdminGrantRequestResponse>> ListAsync(
        Guid actorId,
        AdminGrantStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var result = await grants.SearchRequestsAsync(status, page, pageSize, cancellationToken);
        var items = new List<AdminGrantRequestResponse>(result.Items.Count);
        foreach (var request in result.Items)
        {
            items.Add(await ToResponseAsync(request, cancellationToken));
        }

        return new PaginatedItems<AdminGrantRequestResponse>(
            items,
            new(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<AdminGrantRequestResponse> CreateAsync(
        Guid actorId,
        CreateAdminGrantRequest request,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var beneficiaryId = Parse.ParseGuid(request.BeneficiaryId, nameof(request.BeneficiaryId));

        if (beneficiaryId == actorId)
        {
            throw new DomainRuleViolationException("El solicitante no puede ser el beneficiario.");
        }

        var status = await accounts.FindStatusAsync(beneficiaryId, cancellationToken)
            ?? throw new NotFoundException("La cuenta beneficiaria no existe.");
        if (status != AccountStatus.ACTIVE)
        {
            throw new DomainRuleViolationException("El beneficiario debe tener una cuenta ACTIVE.");
        }

        if (await grants.HasPendingRequestAsync(beneficiaryId, cancellationToken))
        {
            throw new ConflictException("Ya existe una solicitud pendiente para esta persona.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var created = new AdminGrantRequest
        {
            BeneficiaryId = beneficiaryId,
            RequestedBy = actorId,
            Status = AdminGrantStatus.PENDING,
        };

        await grants.AddRequestAsync(created, cancellationToken);
        await audit.RecordAsync(
            actorId,
            "ADMIN_GRANT_REQUESTED",
            "ADMIN_GRANT",
            created.Id,
            new Dictionary<string, object?> { ["beneficiaryId"] = beneficiaryId },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(created, cancellationToken);
    }

    public async Task<AdminGrantRequestResponse> ApproveAsync(
        Guid actorId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var request = await grants.FindRequestAsync(requestId, cancellationToken)
            ?? throw new NotFoundException("La solicitud no existe.");
        if (request.Status != AdminGrantStatus.PENDING)
        {
            throw new ConflictException("La solicitud ya fue resuelta.");
        }

        var approvals = await grants.ListApprovalsAsync(requestId, cancellationToken);
        if (approvals.Any(a => a.AdminId == actorId))
        {
            throw new ConflictException("Ya registraste tu aprobación en esta solicitud.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var approval = new AdminGrantApproval { RequestId = requestId, AdminId = actorId, ApprovedAt = now };
        await grants.AddApprovalAsync(approval, cancellationToken);

        var approvers = approvals.Select(a => a.AdminId).Append(actorId).Distinct().ToList();
        if (approvers.Count >= AdminGrantPolicy.MinimumActiveAdmins)
        {
            var activeAdmins = (await roles.ListActiveAdminUserIdsAsync(now, cancellationToken)).ToHashSet();
            AdminGrantPolicy.EnsureCanApprove(request.BeneficiaryId, approvers[0], approvers[1], activeAdmins);

            var assignment = RoleAssignment.Assign(
                request.BeneficiaryId,
                AssignedRole.ADMIN,
                RoleScope.GLOBAL,
                null,
                actorId);
            await roles.AddAsync(assignment, cancellationToken);
            request.Status = AdminGrantStatus.APPROVED;
            request.ResolvedAt = now;
            await audit.RecordRoleAssignedAsync(actorId, assignment, now, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(request, cancellationToken);
    }

    public async Task<AdminGrantRequestResponse> RejectAsync(
        Guid actorId,
        Guid requestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var request = await grants.FindRequestAsync(requestId, cancellationToken)
            ?? throw new NotFoundException("La solicitud no existe.");
        if (request.Status != AdminGrantStatus.PENDING)
        {
            throw new ConflictException("La solicitud ya fue resuelta.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleViolationException("El motivo del rechazo es obligatorio.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        request.Status = AdminGrantStatus.REJECTED;
        request.RejectionReason = reason.Trim();
        request.ResolvedAt = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await ToResponseAsync(request, cancellationToken);
    }

    private async Task<AdminGrantRequestResponse> ToResponseAsync(
        AdminGrantRequest request,
        CancellationToken cancellationToken)
    {
        var approvals = await grants.ListApprovalsAsync(request.Id, cancellationToken);
        return new AdminGrantRequestResponse(
            request.Id,
            request.BeneficiaryId,
            request.RequestedBy,
            request.Status.ToString(),
            approvals.Select(a => new AdminGrantApprovalResponse(a.AdminId, a.ApprovedAt)).ToList(),
            request.RejectionReason,
            request.CreatedAt,
            request.ResolvedAt);
    }
}
