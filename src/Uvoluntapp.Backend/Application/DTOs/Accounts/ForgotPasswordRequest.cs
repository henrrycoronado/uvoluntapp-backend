using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.DTOs.Accounts;

public sealed record ForgotPasswordRequest
{
    public ForgotPasswordRequest()
    {
    }

    public ForgotPasswordRequest(string email) => Email = email;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
