using System.ComponentModel.DataAnnotations;
using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

public static class RefreshTokenRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
