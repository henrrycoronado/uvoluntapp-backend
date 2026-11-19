using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Programs;
using Uvoluntapp.Backend.Application.Services.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Presentation.Controllers;

/// <summary>Catálogo público sin sesión (SCRUM-52) y periodos para sus filtros.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1")]
public sealed class CatalogController(CatalogService catalog) : ControllerBase
{
    [HttpGet("catalog/programs")]
    public async Task<ActionResult<PaginatedItems<CatalogProgramSummary>>> Programs(
        [FromQuery] Guid? periodId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await catalog.ProgramsAsync(periodId, page, pageSize, cancellationToken));

    [HttpGet("catalog/activities")]
    public async Task<ActionResult<PaginatedItems<CatalogActivitySummary>>> Activities(
        [FromQuery] Guid? periodId,
        [FromQuery] Guid? programId,
        [FromQuery] ActivityStatus? status,
        [FromQuery] bool? independent,
        [FromQuery] string? groupLabel,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await catalog.ActivitiesAsync(
            new ActivityQuery(programId, periodId, status, independent, groupLabel, fromDate, toDate, page, pageSize),
            cancellationToken));

    [HttpGet("catalog/periods")]
    public async Task<ActionResult<IReadOnlyList<AcademicPeriodResponse>>> Periods(CancellationToken cancellationToken) =>
        Ok(await catalog.PeriodsAsync(cancellationToken));

    // Solo lectura en M2: los periodos se siembran por configuración (SCRUM-46).
    [HttpGet("academic-periods")]
    public async Task<ActionResult<IReadOnlyList<AcademicPeriodResponse>>> AcademicPeriods(CancellationToken cancellationToken) =>
        Ok(await catalog.PeriodsAsync(cancellationToken));
}
