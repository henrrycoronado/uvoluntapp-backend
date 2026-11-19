using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Uvoluntapp.Backend.Application.Accounts.Contracts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class SessionRequestValidatorTests
{
    [Fact]
    public void AcceptsValidLogin()
    {
        var request = new LoginRequest { Email = "ana.perez@ucb.edu.bo", Password = "ClaveSegura1" };

        Assert.Empty(LoginRequestValidator.Validate(request));
    }

    [Fact]
    public void RejectsLoginWithInvalidEmail()
    {
        var request = new LoginRequest { Email = "sin-arroba", Password = "ClaveSegura1" };

        AssertHasError(LoginRequestValidator.Validate(request), nameof(LoginRequest.Email));
    }

    [Fact]
    public void RejectsLoginWithoutPassword()
    {
        var request = new LoginRequest { Email = "ana.perez@ucb.edu.bo", Password = string.Empty };

        AssertHasError(LoginRequestValidator.Validate(request), nameof(LoginRequest.Password));
    }

    [Fact]
    public void AcceptsRefreshToken()
    {
        var request = new RefreshTokenRequest { RefreshToken = "token-opaco" };

        Assert.Empty(RefreshTokenRequestValidator.Validate(request));
    }

    [Fact]
    public void RejectsEmptyRefreshToken()
    {
        var request = new RefreshTokenRequest { RefreshToken = string.Empty };

        AssertHasError(RefreshTokenRequestValidator.Validate(request), nameof(RefreshTokenRequest.RefreshToken));
    }

    [Fact]
    public void AcceptsLogoutToken()
    {
        var request = new LogoutRequest { RefreshToken = "token-opaco" };

        Assert.Empty(LogoutRequestValidator.Validate(request));
    }

    [Fact]
    public void RejectsEmptyLogoutToken()
    {
        var request = new LogoutRequest { RefreshToken = string.Empty };

        AssertHasError(LogoutRequestValidator.Validate(request), nameof(LogoutRequest.RefreshToken));
    }

    [Fact]
    public void AcceptsResendOtpEmail()
    {
        var request = new ResendOtpRequest { Email = "ana.perez@gmail.com" };

        Assert.Empty(ResendOtpRequestValidator.Validate(request));
    }

    [Fact]
    public void RejectsInvalidResendOtpEmail()
    {
        var request = new ResendOtpRequest { Email = "sin-arroba" };

        AssertHasError(ResendOtpRequestValidator.Validate(request), nameof(ResendOtpRequest.Email));
    }

    private static void AssertHasError(IReadOnlyList<ValidationResult> errors, string memberName)
    {
        Assert.Contains(errors, error => error.MemberNames.Contains(memberName));
    }
}
