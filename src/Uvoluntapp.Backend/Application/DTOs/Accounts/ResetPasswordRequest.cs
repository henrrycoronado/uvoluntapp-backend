using System.ComponentModel.DataAnnotations;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

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
    [MaxLength(FieldLimits.Password)]
    public string NewPassword { get; init; } = string.Empty;
}
