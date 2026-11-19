using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Audit;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit-events")]
public sealed class AuditEventsController(AdminAccessGuard guard, AuditQueryService audit, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedAuditEvents>> List(
        [FromQuery] Guid? actorId,
        [FromQuery] string? action,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var actor = CurrentUser.Id(User);
        var roles = await guard.RequireGlobalAdminAsync(actor, cancellationToken);
        var filter = new AuditEventFilter(actorId, action, entityType, entityId, from, to, page, pageSize);
        return Ok(await audit.ListAsync(filter, actor, roles, clock.GetUtcNow().UtcDateTime, cancellationToken));
    }
}
