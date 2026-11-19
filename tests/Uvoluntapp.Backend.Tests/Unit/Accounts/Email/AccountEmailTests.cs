using System;
using Uvoluntapp.Backend.Domain.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Email;

public class AccountEmailTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ConfirmEmailChangeWithAValidCodeUpdatesTheAddressAndMarksItVerified()
    {
        var account = Account("ana@example.com");
        var pending = account.RequestEmailChange("nueva@example.com", "123456", _now);

        account.ConfirmEmailChange(pending, "123456", _now.AddMinutes(1));

        Assert.Equal("nueva@example.com", account.Email);
        Assert.Equal(_now.AddMinutes(1).UtcDateTime, account.EmailVerifiedAt);
    }

    [Fact]
    public void ConfirmEmailChangeWithAnInvalidCodeKeepsTheCurrentAddress()
    {
        var account = Account("ana@example.com");
        var previousVerification = account.EmailVerifiedAt;
        var pending = account.RequestEmailChange("nueva@example.com", "123456", _now);

        var exception = Assert.Throws<EmailVerificationRejectedException>(
            () => account.ConfirmEmailChange(pending, "654321", _now.AddMinutes(1)));

        Assert.Equal(OneTimeCodeRejectionReason.INVALID_CODE, exception.Reason);
        Assert.Equal("ana@example.com", account.Email);
        Assert.Equal(previousVerification, account.EmailVerifiedAt);
    }

    [Fact]
    public void RequestEmailChangeRejectsTheCurrentAddress()
    {
        var account = Account("ana@example.com");

        Assert.Throws<DomainRuleViolationException>(
            () => account.RequestEmailChange(" ANA@Example.com ", "123456", _now));
    }

    [Theory]
    [InlineData("ana@example.com")]
    [InlineData("ana@gmail.com")]
    [InlineData("ana@sub.ucb.edu.bo")]
    [InlineData("ana@fakeucb.edu.bo")]
    public void RequestInstitutionalEmailRejectsOtherDomains(string email)
    {
        var account = Account();

        Assert.Throws<DomainRuleViolationException>(
            () => account.RequestInstitutionalEmail(email, "123456", _now));
    }

    [Fact]
    public void ConfirmInstitutionalEmailMarksItVerifiedAndEnablesHourCommitments()
    {
        var account = Account();
        var pending = account.RequestInstitutionalEmail("ana.perez@ucb.edu.bo", "123456", _now);

        Assert.False(account.CanHaveHourCommitments);

        account.ConfirmInstitutionalEmail(pending, "123456", _now.AddMinutes(1));

        Assert.Equal("ana.perez@ucb.edu.bo", account.InstitutionalEmail);
        Assert.Equal(_now.AddMinutes(1).UtcDateTime, account.InstitutionalEmailVerifiedAt);
        Assert.True(account.HasVerifiedInstitutionalEmail);
        Assert.True(account.CanHaveHourCommitments);
    }

    [Fact]
    public void ConfirmInstitutionalEmailRejectsAPendingExternalAddress()
    {
        var account = Account();
        var external = PendingEmailVerification.Create(
            "ana@gmail.com",
            EmailVerificationPurpose.INSTITUTIONAL_EMAIL,
            "123456",
            _now);

        Assert.Throws<DomainRuleViolationException>(
            () => account.ConfirmInstitutionalEmail(external, "123456", _now.AddMinutes(1)));
        Assert.Null(account.InstitutionalEmail);
    }

    [Fact]
    public void ConfirmEmailChangeRejectsAChallengeForAnotherPurpose()
    {
        var account = Account();
        var institutional = PendingEmailVerification.Create(
            "ana.perez@ucb.edu.bo",
            EmailVerificationPurpose.INSTITUTIONAL_EMAIL,
            "123456",
            _now);

        Assert.Throws<DomainRuleViolationException>(
            () => account.ConfirmEmailChange(institutional, "123456", _now.AddMinutes(1)));
        Assert.Equal("ana@example.com", account.Email);
    }

    private static AccountEmail Account(string email = "ana@example.com") =>
        new()
        {
            UserId = Guid.NewGuid(),
            Email = email,
            EmailVerifiedAt = _now.AddDays(-1).UtcDateTime,
        };
}
