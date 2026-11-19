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
[Route("api/v1/users")]
public sealed class UsersController(UserAdministrationService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedItems<UserSummaryResponse>>> List(
        [FromQuery] AccountStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await users.ListAsync(CurrentUser.Id(User), status, search, page, pageSize, cancellationToken));

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<UserDetailResponse>> Get(Guid userId, CancellationToken cancellationToken) =>
        Ok(await users.GetAsync(CurrentUser.Id(User), userId, cancellationToken));

    [HttpPost("{userId:guid}/suspend")]
    public async Task<ActionResult<MessageResponse>> Suspend(Guid userId, ReasonRequest request, CancellationToken cancellationToken)
    {
        await users.SuspendAsync(CurrentUser.Id(User), userId, request.Reason, cancellationToken);
        return Ok(new MessageResponse { Message = "Cuenta suspendida." });
    }

    [HttpPost("{userId:guid}/verify")]
    public async Task<ActionResult<MessageResponse>> Verify(Guid userId, CancellationToken cancellationToken)
    {
        await users.VerifyAsync(CurrentUser.Id(User), userId, cancellationToken);
        return Ok(new MessageResponse { Message = "Cuenta verificada." });
    }

    [HttpPost("{userId:guid}/change-email")]
    public async Task<ActionResult<MessageResponse>> ChangeEmail(Guid userId, ChangeUserEmailRequest request, CancellationToken cancellationToken)
    {
        await users.ChangeEmailAsync(CurrentUser.Id(User), userId, request, cancellationToken);
        return Ok(new MessageResponse { Message = "Correo de acceso actualizado. La persona debe recuperar su contraseña con el correo nuevo." });
    }

    [HttpPost("{userId:guid}/reactivate")]
    public async Task<ActionResult<MessageResponse>> Reactivate(Guid userId, CancellationToken cancellationToken)
    {
        await users.ReactivateAsync(CurrentUser.Id(User), userId, cancellationToken);
        return Ok(new MessageResponse { Message = "Cuenta reactivada." });
    }
}
