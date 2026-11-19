using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public static class CompleteRegistrationRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(CompleteRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
