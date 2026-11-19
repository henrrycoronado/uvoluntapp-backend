using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounts/approval-requests")]
public sealed class AccountApprovalsController(AccountApprovalAdminService approvals) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedItems<ApprovalRequestSummary>>> List(
        [FromQuery] AccountApprovalStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await approvals.ListAsync(CurrentUser.Id(User), status, page, pageSize, cancellationToken));

    [HttpPost("{requestId:guid}/approve")]
    public async Task<ActionResult<MessageResponse>> Approve(Guid requestId, CancellationToken cancellationToken)
    {
        await approvals.ApproveAsync(CurrentUser.Id(User), requestId, cancellationToken);
        return Ok(new MessageResponse { Message = "Cuenta aprobada." });
    }

    [HttpPost("{requestId:guid}/reject")]
    public async Task<ActionResult<MessageResponse>> Reject(Guid requestId, ReasonRequest request, CancellationToken cancellationToken)
    {
        await approvals.RejectAsync(CurrentUser.Id(User), requestId, request.Reason, cancellationToken);
        return Ok(new MessageResponse { Message = "Cuenta rechazada." });
    }
}
