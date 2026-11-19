using System.ComponentModel.DataAnnotations;

using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

public static class ResendOtpRequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate(ResendOtpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return DataAnnotationsValidation.Validate(request);
    }
}
