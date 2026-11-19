using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.Services.Activities;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

/// <summary>Actividades (SCRUM-47 a 51, 53, 54, 123). La autorización por recurso la decide PlanningAccess.</summary>
[ApiController]
[Authorize]
[Route("api/v1/activities")]
public sealed class ActivitiesController(ActivityService activities) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedItems<ActivityResponse>>> List(
        [FromQuery] Guid? programId,
        [FromQuery] Guid? periodId,
        [FromQuery] ActivityStatus? status,
        [FromQuery] bool? independent,
        [FromQuery] string? groupLabel,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await activities.ListAsync(
            CurrentUser.Id(User),
            new ActivityQuery(programId, periodId, status, independent, groupLabel, fromDate, toDate, page, pageSize),
            cancellationToken));

    [HttpGet("{activityId:guid}")]
    public async Task<ActionResult<ActivityResponse>> Get(Guid activityId, CancellationToken cancellationToken) =>
        Ok(await activities.GetAsync(CurrentUser.Id(User), activityId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ActivityResponse>> Create(ActivityRequest request, CancellationToken cancellationToken)
    {
        var activity = await activities.CreateAsync(CurrentUser.Id(User), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { activityId = activity.Id }, activity);
    }

    [HttpPatch("{activityId:guid}")]
    public async Task<ActionResult<ActivityResponse>> Update(Guid activityId, ActivityRequest request, CancellationToken cancellationToken) =>
        Ok(await activities.UpdateAsync(CurrentUser.Id(User), activityId, request, cancellationToken));

    [HttpPost("{activityId:guid}/publish")]
    public async Task<ActionResult<ActivityResponse>> Publish(Guid activityId, CancellationToken cancellationToken) =>
        Ok(await activities.PublishAsync(CurrentUser.Id(User), activityId, cancellationToken));

    [HttpPost("{activityId:guid}/finish")]
    public async Task<ActionResult<ActivityResponse>> Finish(Guid activityId, CancellationToken cancellationToken) =>
        Ok(await activities.FinishAsync(CurrentUser.Id(User), activityId, cancellationToken));

    [HttpPost("{activityId:guid}/cancel")]
    public async Task<ActionResult<ActivityResponse>> Cancel(Guid activityId, ReasonRequest request, CancellationToken cancellationToken) =>
        Ok(await activities.CancelAsync(CurrentUser.Id(User), activityId, request.Reason, cancellationToken));

    [HttpDelete("{activityId:guid}/discard")]
    public async Task<IActionResult> Discard(Guid activityId, CancellationToken cancellationToken)
    {
        await activities.DiscardAsync(CurrentUser.Id(User), activityId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{activityId:guid}/reschedules")]
    public async Task<ActionResult<IReadOnlyList<ActivityRescheduleResponse>>> Reschedules(Guid activityId, CancellationToken cancellationToken) =>
        Ok(await activities.ListReschedulesAsync(CurrentUser.Id(User), activityId, cancellationToken));

    [HttpPost("{activityId:guid}/reschedules")]
    public async Task<ActionResult<RescheduleActivityResponse>> Reschedule(Guid activityId, RescheduleActivityRequest request, CancellationToken cancellationToken) =>
        Ok(await activities.RescheduleAsync(CurrentUser.Id(User), activityId, request, cancellationToken));

    [HttpPost("{activityId:guid}/move")]
    public async Task<ActionResult<ActivityResponse>> Move(Guid activityId, MoveActivityRequest request, CancellationToken cancellationToken) =>
        Ok(await activities.MoveAsync(CurrentUser.Id(User), activityId, request, cancellationToken));

    [HttpPost("check-conflicts")]
    public async Task<ActionResult<CheckConflictsResponse>> CheckConflicts(CheckConflictsRequest request, CancellationToken cancellationToken) =>
        Ok(await activities.CheckConflictsAsync(CurrentUser.Id(User), request, cancellationToken));
}
