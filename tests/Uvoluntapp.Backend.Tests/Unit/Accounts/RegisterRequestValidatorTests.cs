using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Uvoluntapp.Backend.Application.Accounts.Contracts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class RegisterRequestValidatorTests
{
    [Fact]
    public void AcceptsInstitutionalEmail()
    {
        var request = ValidRequest() with { Email = "ana.perez@ucb.edu.bo" };

        Assert.Empty(RegisterRequestValidator.Validate(request));
    }

    [Fact]
    public void AcceptsExternalEmail()
    {
        var request = ValidRequest() with { Email = "ana.perez@gmail.com" };

        Assert.Empty(RegisterRequestValidator.Validate(request));
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("@ucb.edu.bo")]
    [InlineData("ana.perez@")]
    public void RejectsInvalidEmail(string email)
    {
        var request = ValidRequest() with { Email = email };

        AssertHasError(RegisterRequestValidator.Validate(request), nameof(RegisterRequest.Email));
    }

    [Fact]
    public void RejectsPasswordShorterThanEightCharacters()
    {
        var request = ValidRequest() with { Password = "corta1" };

        AssertHasError(RegisterRequestValidator.Validate(request), nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsMissingName(string value)
    {
        var request = ValidRequest() with { FirstName = value, LastName = value };

        var errors = RegisterRequestValidator.Validate(request);

        AssertHasError(errors, nameof(RegisterRequest.FirstName));
        AssertHasError(errors, nameof(RegisterRequest.LastName));
    }

    [Fact]
    public void RejectsNullRequest()
    {
        Assert.Throws<ArgumentNullException>(() => RegisterRequestValidator.Validate(null!));
    }

    private static RegisterRequest ValidRequest() => new()
    {
        Email = "ana.perez@ucb.edu.bo",
        Password = "ClaveSegura1",
        FirstName = "Ana",
        LastName = "Pérez",
    };

    private static void AssertHasError(IReadOnlyList<ValidationResult> errors, string memberName)
    {
        Assert.Contains(errors, error => error.MemberNames.Contains(memberName));
    }
}
