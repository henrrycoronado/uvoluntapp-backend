using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public static class RefreshTokenRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(RefreshTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
