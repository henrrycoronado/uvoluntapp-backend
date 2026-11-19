using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.Administration.Contracts;
using Uvoluntapp.Backend.Application.Programs.Contracts;
using Uvoluntapp.Backend.Application.Programs.Services;
using Uvoluntapp.Backend.Domain.Programs;
using Uvoluntapp.Backend.Presentation.Common;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/programs")]
public sealed class ProgramsController(ProgramQueryService programs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedItems<ProgramResponse>>> List(
        [FromQuery] ProgramStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await programs.ListAsync(CurrentUser.Id(User), status, page, pageSize, cancellationToken));
}
