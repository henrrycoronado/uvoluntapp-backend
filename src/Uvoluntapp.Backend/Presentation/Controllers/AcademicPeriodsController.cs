using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Programs;
using Uvoluntapp.Backend.Application.Services.Programs;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/academic-periods/{periodId:guid}")]
public sealed class AcademicPeriodsController(PeriodAdministration periods) : ControllerBase
{
    [HttpPut]
    public async Task<ActionResult<AcademicPeriodResponse>> Edit(Guid periodId, PeriodEditRequest request, CancellationToken ct) =>
        Ok(await periods.ChangeAsync(CurrentUser.Id(User), periodId, request.Label, request.EndDate, false, ct));

    [HttpPost("close")]
    public async Task<ActionResult<AcademicPeriodResponse>> Close(Guid periodId, PeriodCloseRequest request, CancellationToken ct) =>
        Ok(await periods.ChangeAsync(CurrentUser.Id(User), periodId, request.SuccessorLabel, null, true, ct));
}
