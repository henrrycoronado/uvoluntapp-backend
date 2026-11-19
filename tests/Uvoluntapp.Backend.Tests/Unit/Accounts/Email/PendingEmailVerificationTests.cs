using System;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Email;

public class PendingEmailVerificationTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateKeepsThePurposeAndHashesTheCode()
    {
        var pending = PendingEmailVerification.Create(
            " Ana@Example.com ",
            EmailVerificationPurpose.EMAIL_CHANGE,
            "123456",
            _now);

        Assert.Equal("ana@example.com", pending.Email);
        Assert.Equal(EmailVerificationPurpose.EMAIL_CHANGE, pending.Purpose);
        Assert.NotEqual("123456", pending.Code.CodeHash);
        Assert.Equal(_now.AddMinutes(10), pending.ExpiresAt);
    }

    [Fact]
    public void RestoredChallengeStillVerifiesTheCode()
    {
        var pending = PendingEmailVerification.Create(
            "ana@example.com",
            EmailVerificationPurpose.INSTITUTIONAL_EMAIL,
            "123456",
            _now);

        var restored = PendingEmailVerification.Restore(
            pending.Email,
            pending.Purpose,
            OneTimeCode.Restore(
                pending.Code.CodeHash,
                pending.Code.ExpiresAt,
                pending.Code.Attempts,
                pending.Code.IsUsed),
            pending.RequestedAt);

        var result = restored.Verify("123456", _now.AddMinutes(1));

        Assert.True(result.IsValid);
        Assert.Equal(EmailVerificationPurpose.INSTITUTIONAL_EMAIL, restored.Purpose);
    }

    [Fact]
    public void WrongCodeIsRejectedAndCountsTheAttempt()
    {
        var pending = PendingEmailVerification.Create(
            "ana@example.com",
            EmailVerificationPurpose.EMAIL_CHANGE,
            "123456",
            _now);

        var result = pending.Verify("654321", _now.AddMinutes(1));

        Assert.False(result.IsValid);
        Assert.Equal(OneTimeCodeRejectionReason.INVALID_CODE, result.RejectionReason);
        Assert.Equal(1, pending.Code.Attempts);
    }
}
