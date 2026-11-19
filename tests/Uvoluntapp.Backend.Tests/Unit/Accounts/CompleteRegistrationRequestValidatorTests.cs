using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Xunit;

using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Validators.Accounts;
namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class CompleteRegistrationRequestValidatorTests
{
    [Fact]
    public void AcceptsCompleteExternalRequest()
    {
        Assert.Empty(CompleteRegistrationRequestValidator.Validate(ValidRequest()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsMissingPhone(string phone)
    {
        var request = ValidRequest() with { Phone = phone };

        AssertHasError(CompleteRegistrationRequestValidator.Validate(request), nameof(CompleteRegistrationRequest.Phone));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsMissingNames(string value)
    {
        var request = ValidRequest() with { FirstName = value, LastName = value };

        var errors = CompleteRegistrationRequestValidator.Validate(request);

        AssertHasError(errors, nameof(CompleteRegistrationRequest.FirstName));
        AssertHasError(errors, nameof(CompleteRegistrationRequest.LastName));
    }

    [Fact]
    public void RejectsMissingPastoralRelation()
    {
        var request = ValidRequest() with { RelationWithPastoral = string.Empty };

        AssertHasError(
            CompleteRegistrationRequestValidator.Validate(request),
            nameof(CompleteRegistrationRequest.RelationWithPastoral));
    }

    [Fact]
    public void RejectsInvalidEmail()
    {
        var request = ValidRequest() with { Email = "sin-arroba" };

        AssertHasError(CompleteRegistrationRequestValidator.Validate(request), nameof(CompleteRegistrationRequest.Email));
    }

    private static CompleteRegistrationRequest ValidRequest() => new()
    {
        Email = "ana.perez@gmail.com",
        FirstName = "Ana",
        LastName = "Pérez",
        Phone = "+591 70000000",
        RelationWithPastoral = "Voluntaria externa",
    };

    private static void AssertHasError(IReadOnlyList<ValidationResult> errors, string memberName)
    {
        Assert.Contains(errors, error => error.MemberNames.Contains(memberName));
    }
}
