using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.Services.Activities;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class EnrollmentsController(EnrollmentService service) : ControllerBase
{
    [HttpPost("activities/{activityId:guid}/enrollments")]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(Guid activityId, EnrollmentRequest request, CancellationToken ct) =>
        Ok(await service.EnrollAsync(CurrentUser.Id(User), activityId, request.GroupId, ct));

    [HttpDelete("activities/{activityId:guid}/enrollments/{enrollmentId:guid}")]
    public async Task<ActionResult<EnrollmentResponse>> Cancel(Guid activityId, Guid enrollmentId, CancellationToken ct) =>
        Ok(await service.CancelAsync(CurrentUser.Id(User), activityId, enrollmentId, ct));

    [HttpGet("me/enrollments")]
    public async Task<ActionResult<List<EnrollmentResponse>>> Mine(CancellationToken ct) =>
        Ok(await service.ListAsync(CurrentUser.Id(User), null, ct));

    [HttpGet("activities/{activityId:guid}/enrollments")]
    public async Task<ActionResult<List<EnrollmentResponse>>> List(Guid activityId, CancellationToken ct) =>
        Ok(await service.ListAsync(CurrentUser.Id(User), activityId, ct));
}
