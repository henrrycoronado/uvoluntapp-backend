using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public static class VerifyOtpRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(VerifyOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
