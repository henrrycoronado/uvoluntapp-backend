using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Validators.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class VerifyOtpRequestValidatorTests
{
    [Fact]
    public void AcceptsSixDigitCode()
    {
        var request = new VerifyOtpRequest { Email = "ana.perez@ucb.edu.bo", Code = "012345" };

        Assert.Empty(VerifyOtpRequestValidator.Validate(request));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    [InlineData("12345a")]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsInvalidCode(string code)
    {
        var request = new VerifyOtpRequest { Email = "ana.perez@ucb.edu.bo", Code = code };

        AssertHasError(VerifyOtpRequestValidator.Validate(request), nameof(VerifyOtpRequest.Code));
    }

    [Fact]
    public void RejectsInvalidEmail()
    {
        var request = new VerifyOtpRequest { Email = "sin-arroba", Code = "123456" };

        AssertHasError(VerifyOtpRequestValidator.Validate(request), nameof(VerifyOtpRequest.Email));
    }

    private static void AssertHasError(IReadOnlyList<ValidationResult> errors, string memberName)
    {
        Assert.Contains(errors, error => error.MemberNames.Contains(memberName));
    }
}
