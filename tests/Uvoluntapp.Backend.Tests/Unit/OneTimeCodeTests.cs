using System.Security.Cryptography;
using System.Text;
using System;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit;

public class OneTimeCodeTests
{
    private static readonly DateTimeOffset _createdAt = new(2026, 11, 19, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AcceptsTheCorrectCodeAndStoresOnlyItsHash()
    {
        var oneTimeCode = OneTimeCode.Create("123456", _createdAt);

        var result = oneTimeCode.Verify("123456", _createdAt.AddMinutes(1));

        Assert.True(result.IsValid);
        Assert.Null(result.RejectionReason);
        Assert.True(oneTimeCode.IsUsed);
        Assert.NotEqual("123456", oneTimeCode.CodeHash);
        Assert.Equal(
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("123456"))),
            oneTimeCode.CodeHash);
    }

    [Fact]
    public void RejectsAnIncorrectCodeAndCountsTheAttempt()
    {
        var oneTimeCode = OneTimeCode.Create("123456", _createdAt);

        var result = oneTimeCode.Verify("654321", _createdAt.AddMinutes(1));

        Assert.False(result.IsValid);
        Assert.Equal(OneTimeCodeRejectionReason.INVALID_CODE, result.RejectionReason);
        Assert.Equal(1, oneTimeCode.Attempts);
    }

    [Fact]
    public void RejectsAnExpiredCode()
    {
        var oneTimeCode = OneTimeCode.Create("123456", _createdAt);

        var result = oneTimeCode.Verify("123456", _createdAt.AddMinutes(10));

        Assert.False(result.IsValid);
        Assert.Equal(OneTimeCodeRejectionReason.EXPIRED, result.RejectionReason);
    }

    [Fact]
    public void RejectsTheSixthAttempt()
    {
        var oneTimeCode = OneTimeCode.Create("123456", _createdAt);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            oneTimeCode.Verify("654321", _createdAt.AddMinutes(1));
        }

        var result = oneTimeCode.Verify("123456", _createdAt.AddMinutes(1));

        Assert.False(result.IsValid);
        Assert.Equal(OneTimeCodeRejectionReason.ATTEMPTS_EXHAUSTED, result.RejectionReason);
        Assert.Equal(5, oneTimeCode.Attempts);
    }

    [Fact]
    public void RejectsReuseAfterSuccessfulVerification()
    {
        var oneTimeCode = OneTimeCode.Create("123456", _createdAt);

        oneTimeCode.Verify("123456", _createdAt.AddMinutes(1));
        var result = oneTimeCode.Verify("123456", _createdAt.AddMinutes(2));

        Assert.False(result.IsValid);
        Assert.Equal(OneTimeCodeRejectionReason.ALREADY_USED, result.RejectionReason);
    }
}
