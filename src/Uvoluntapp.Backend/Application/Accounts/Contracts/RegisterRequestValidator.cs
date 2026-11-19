using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public static class RegisterRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
