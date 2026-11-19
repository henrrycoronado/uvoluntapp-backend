using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public static class LogoutRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(LogoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
