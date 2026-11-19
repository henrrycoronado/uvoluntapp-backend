using System.ComponentModel.DataAnnotations;

using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

public static class LogoutRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(LogoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
