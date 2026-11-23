using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.Services.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class ImagesController(ResourceImageService images) : ControllerBase
{
    [HttpGet("me/profile-image")]
    public async Task<IActionResult> ReadProfile([FromQuery] bool thumbnail, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Redirect(await images.ReadAsync(User.Identity?.IsAuthenticated == true ? CurrentUser.Id(User) : null, ImageOwner.Profile, CurrentUser.Id(User), thumbnail, ct));
    }

    [HttpPut("me/profile-image")]
    [HttpPost("me/profile-image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ReplaceProfile([FromForm] IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        await images.ReplaceAsync(CurrentUser.Id(User), ImageOwner.Profile, CurrentUser.Id(User), stream, file.ContentType, ct);
        return NoContent();
    }

    [HttpDelete("me/profile-image")]
    public async Task<IActionResult> RemoveProfile(CancellationToken ct)
    {
        await images.ReplaceAsync(CurrentUser.Id(User), ImageOwner.Profile, CurrentUser.Id(User), null, null, ct);
        return NoContent();
    }

    [HttpGet("programs/{id:guid}/cover")]
    [AllowAnonymous]
    public async Task<IActionResult> ReadProgram(Guid id, [FromQuery] bool thumbnail, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Redirect(await images.ReadAsync(User.Identity?.IsAuthenticated == true ? CurrentUser.Id(User) : null, ImageOwner.Program, id, thumbnail, ct));
    }

    [HttpPut("programs/{id:guid}/cover")]
    [HttpPost("programs/{id:guid}/cover")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ReplaceProgram(Guid id, [FromForm] IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        await images.ReplaceAsync(CurrentUser.Id(User), ImageOwner.Program, id, stream, file.ContentType, ct);
        return NoContent();
    }

    [HttpDelete("programs/{id:guid}/cover")]
    public async Task<IActionResult> RemoveProgram(Guid id, CancellationToken ct)
    {
        await images.ReplaceAsync(CurrentUser.Id(User), ImageOwner.Program, id, null, null, ct);
        return NoContent();
    }

    [HttpGet("activities/{id:guid}/image")]
    [AllowAnonymous]
    public async Task<IActionResult> ReadActivity(Guid id, [FromQuery] bool thumbnail, CancellationToken ct)
    {
        Response.Headers.CacheControl = "private, no-store";
        return Redirect(await images.ReadAsync(User.Identity?.IsAuthenticated == true ? CurrentUser.Id(User) : null, ImageOwner.Activity, id, thumbnail, ct));
    }

    [HttpPut("activities/{id:guid}/image")]
    [HttpPost("activities/{id:guid}/image")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> ReplaceActivity(Guid id, [FromForm] IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        await images.ReplaceAsync(CurrentUser.Id(User), ImageOwner.Activity, id, stream, file.ContentType, ct);
        return NoContent();
    }

    [HttpDelete("activities/{id:guid}/image")]
    public async Task<IActionResult> RemoveActivity(Guid id, CancellationToken ct)
    {
        await images.ReplaceAsync(CurrentUser.Id(User), ImageOwner.Activity, id, null, null, ct);
        return NoContent();
    }
}
