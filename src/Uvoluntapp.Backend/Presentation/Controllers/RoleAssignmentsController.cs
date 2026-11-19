using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.Administration.Contracts;
using Uvoluntapp.Backend.Application.Administration.Services;
using Uvoluntapp.Backend.Domain.Accounts;
using Uvoluntapp.Backend.Presentation.Common;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/role-assignments")]
public sealed class RoleAssignmentsController(RoleAssignmentService roles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedItems<RoleAssignmentResponse>>> List(
        [FromQuery] AssignedRole? role,
        [FromQuery] RoleScope? scope,
        [FromQuery] Guid? userId,
        [FromQuery] bool? active,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await roles.ListAsync(CurrentUser.Id(User), role, scope, userId, active, page, pageSize, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoleAssignmentResponse>> Create(CreateRoleAssignmentRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await roles.CreateAsync(CurrentUser.Id(User), request, cancellationToken));

    [HttpGet("{assignmentId:guid}")]
    public async Task<ActionResult<RoleAssignmentResponse>> Get(Guid assignmentId, CancellationToken cancellationToken) =>
        Ok(await roles.GetAsync(CurrentUser.Id(User), assignmentId, cancellationToken));

    [HttpPost("{assignmentId:guid}/revoke")]
    public async Task<ActionResult<RoleAssignmentResponse>> Revoke(Guid assignmentId, CancellationToken cancellationToken) =>
        Ok(await roles.RevokeAsync(CurrentUser.Id(User), assignmentId, cancellationToken));
}
