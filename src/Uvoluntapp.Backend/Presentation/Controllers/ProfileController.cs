using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.DTOs.Profiles;
using Uvoluntapp.Backend.Application.Services.Profiles;
using Uvoluntapp.Backend.Presentation.Helpers;

namespace Uvoluntapp.Backend.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/profile")]
public sealed class ProfileController(ProfileApplicationService profile) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileDetailResponse>> Get(CancellationToken cancellationToken) =>
        Ok(await profile.GetAsync(CurrentUser.Id(User), cancellationToken));

    [HttpPatch]
    public async Task<ActionResult<ProfileDetailResponse>> Update(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profile.UpdateAsync(CurrentUser.Id(User), request, cancellationToken));

    [HttpPost("change-email")]
    public async Task<ActionResult<MessageResponse>> ChangeEmail(ChangeEmailRequest request, CancellationToken cancellationToken) =>
        Ok(new MessageResponse { Message = await profile.RequestEmailChangeAsync(CurrentUser.Id(User), request, cancellationToken) });

    [HttpPost("verify-email-change")]
    public async Task<ActionResult<MessageResponse>> VerifyEmailChange(VerifyCodeRequest request, CancellationToken cancellationToken) =>
        Ok(new MessageResponse { Message = await profile.VerifyEmailChangeAsync(CurrentUser.Id(User), request, cancellationToken) });

    [HttpPost("set-institutional-email")]
    public async Task<ActionResult<MessageResponse>> SetInstitutionalEmail(SetInstitutionalEmailRequest request, CancellationToken cancellationToken) =>
        Ok(new MessageResponse { Message = await profile.SetInstitutionalEmailAsync(CurrentUser.Id(User), request, cancellationToken) });

    [HttpPost("verify-institutional-email")]
    public async Task<ActionResult<MessageResponse>> VerifyInstitutionalEmail(VerifyCodeRequest request, CancellationToken cancellationToken) =>
        Ok(new MessageResponse { Message = await profile.VerifyInstitutionalEmailAsync(CurrentUser.Id(User), request, cancellationToken) });
}
