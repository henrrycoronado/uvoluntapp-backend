using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.Administration.Contracts;
using Uvoluntapp.Backend.Application.Administration.Services;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Presentation.Common;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin-grants")]
public sealed class AdminGrantsController(AdminGrantService grants) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedItems<AdminGrantRequestResponse>>> List(
        [FromQuery] AdminGrantStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await grants.ListAsync(CurrentUser.Id(User), status, page, pageSize, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AdminGrantRequestResponse>> Create(CreateAdminGrantRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await grants.CreateAsync(CurrentUser.Id(User), request, cancellationToken));

    [HttpPost("{grantId:guid}/approve")]
    public async Task<ActionResult<AdminGrantRequestResponse>> Approve(Guid grantId, CancellationToken cancellationToken) =>
        Ok(await grants.ApproveAsync(CurrentUser.Id(User), grantId, cancellationToken));

    [HttpPost("{grantId:guid}/reject")]
    public async Task<ActionResult<AdminGrantRequestResponse>> Reject(Guid grantId, ReasonRequest request, CancellationToken cancellationToken) =>
        Ok(await grants.RejectAsync(CurrentUser.Id(User), grantId, request.Reason, cancellationToken));
}
