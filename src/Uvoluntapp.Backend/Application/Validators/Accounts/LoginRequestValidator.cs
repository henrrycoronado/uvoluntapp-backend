using System.ComponentModel.DataAnnotations;
using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

public static class LoginRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
