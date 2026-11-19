using System.ComponentModel.DataAnnotations;

using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

public static class CompleteRegistrationRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(CompleteRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
