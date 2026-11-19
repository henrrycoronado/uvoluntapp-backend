using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Programs;
using Uvoluntapp.Backend.Application.Services.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

/// <summary>Programas (SCRUM-45, 46, 103) y su equipo (SCRUM-122). La autorización por recurso la decide PlanningAccess.</summary>
[ApiController]
[Authorize]
[Route("api/v1/programs")]
public sealed class ProgramsController(ProgramService programs) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedItems<ProgramResponse>>> List(
        [FromQuery] ProgramStatus? status,
        [FromQuery] Guid? periodId,
        [FromQuery] bool includeArchived = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await programs.ListAsync(CurrentUser.Id(User), status, includeArchived, periodId, page, pageSize, cancellationToken));

    [HttpGet("{programId:guid}")]
    public async Task<ActionResult<ProgramResponse>> Get(Guid programId, CancellationToken cancellationToken) =>
        Ok(await programs.GetAsync(CurrentUser.Id(User), programId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProgramResponse>> Create(ProgramDetailsRequest request, CancellationToken cancellationToken)
    {
        var program = await programs.CreateAsync(CurrentUser.Id(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { programId = program.Id }, program);
    }

    [HttpPatch("{programId:guid}")]
    public async Task<ActionResult<ProgramResponse>> Update(Guid programId, ProgramDetailsRequest request, CancellationToken cancellationToken) =>
        Ok(await programs.UpdateAsync(CurrentUser.Id(User), programId, request, cancellationToken));

    [HttpPost("{programId:guid}/activate")]
    public async Task<ActionResult<ProgramResponse>> Activate(Guid programId, CancellationToken cancellationToken) =>
        Ok(await programs.ActivateAsync(CurrentUser.Id(User), programId, cancellationToken));

    [HttpPost("{programId:guid}/deactivate")]
    public async Task<ActionResult<ProgramResponse>> Deactivate(Guid programId, CancellationToken cancellationToken) =>
        Ok(await programs.DeactivateAsync(CurrentUser.Id(User), programId, cancellationToken));

    [HttpPost("{programId:guid}/archive")]
    public async Task<ActionResult<MessageResponse>> Archive(Guid programId, ReasonRequest request, CancellationToken cancellationToken)
    {
        await programs.ArchiveAsync(CurrentUser.Id(User), programId, request.Reason, cancellationToken);
        return Ok(new MessageResponse { Message = "Programa archivado." });
    }

    [HttpGet("{programId:guid}/periods")]
    public async Task<ActionResult<IReadOnlyList<AcademicPeriodResponse>>> Periods(Guid programId, CancellationToken cancellationToken) =>
        Ok(await programs.ListPeriodsAsync(CurrentUser.Id(User), programId, cancellationToken));

    [HttpGet("{programId:guid}/team")]
    public async Task<ActionResult<IReadOnlyList<ProgramTeamMember>>> Team(Guid programId, CancellationToken cancellationToken) =>
        Ok(await programs.TeamAsync(CurrentUser.Id(User), programId, cancellationToken));
}
