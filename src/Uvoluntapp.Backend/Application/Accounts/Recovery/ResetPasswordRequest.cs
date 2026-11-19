using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Recovery;

public sealed record ResetPasswordRequest
{
    public ResetPasswordRequest()
    {
    }

    public ResetPasswordRequest(string token, string newPassword)
    {
        Token = token;
        NewPassword = newPassword;
    }

    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
