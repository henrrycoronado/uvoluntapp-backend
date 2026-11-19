using System.ComponentModel.DataAnnotations;
using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

public static class RegisterRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
