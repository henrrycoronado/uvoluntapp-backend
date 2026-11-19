using System.ComponentModel.DataAnnotations;
using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

public static class VerifyOtpRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(VerifyOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
