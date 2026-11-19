using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Uvoluntapp.Backend.Application.Accounts;
using Uvoluntapp.Backend.Application.Accounts.Contracts;

namespace Uvoluntapp.Backend.Presentation.Accounts;

[ApiController]
[Route("api/v1/auth")]
[TypeFilter<AccountExceptionFilter>]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(IRegistration registration, IAccountSessions sessions, RegistrationProof proof) : ControllerBase
{
    private const string OtpMessage = "Si corresponde, recibirás un código en tu correo.";

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<RegisterResponse> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await registration.RegisterAsync(request, ClientIp(), cancellationToken);
        return new RegisterResponse { Message = OtpMessage, RequiresOtp = true };
    }

    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<MessageResponse> ResendOtp(ResendOtpRequest request, CancellationToken cancellationToken)
    {
        await registration.ResendAsync(request.Email, ClientIp(), cancellationToken);
        return new MessageResponse { Message = OtpMessage };
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<VerifyOtpResponse> VerifyOtp(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var response = await registration.VerifyAsync(request, cancellationToken);
        if (response.RequiresApprovalData)
        {
            proof.Issue(Response, request.Email);
        }

        return response;
    }

    [HttpPost("complete-registration")]
    [AllowAnonymous]
    public async Task<MessageResponse> CompleteRegistration(CompleteRegistrationRequest request, CancellationToken cancellationToken)
    {
        proof.Require(Request, request.Email);
        await registration.CompleteAsync(request, cancellationToken);
        proof.Clear(Response);
        return new MessageResponse { Message = "Solicitud de aprobación creada." };
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        sessions.LoginAsync(request, ClientIp(), cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<TokenResponse> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken) =>
        sessions.RefreshAsync(request.RefreshToken, cancellationToken);

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId)
            || !Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sid)?.Value, out var sessionId))
        {
            throw new AccountException(AccountError.InvalidSession);
        }

        await sessions.LogoutAsync(userId, sessionId, request.RefreshToken, cancellationToken);
        return NoContent();
    }

    private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
