using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Validators.Accounts;

internal static class DataAnnotationsValidation
{
    public static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(instance);
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }
}
