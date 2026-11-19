using System;
using System.Threading.Tasks;
using Uvoluntapp.Backend.Application.Accounts.Email;
using Uvoluntapp.Backend.Domain.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts.Email;

public class AccountEmailServiceTests
{
    private static readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset _now = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RequestEmailChangeStoresAPendingVerificationForTheNewAddress()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var result = await service.RequestEmailChangeAsync(
            _userId,
            " Nueva@Example.com ",
            "Secreta123!",
            "123456",
            _now);

        Assert.Equal("nueva@example.com", result.Email);
        Assert.Equal(EmailVerificationPurpose.EMAIL_CHANGE, result.Purpose);
        Assert.Equal(_now.AddMinutes(10), result.ExpiresAt);
        Assert.True(store.Pending.ContainsKey((_userId, EmailVerificationPurpose.EMAIL_CHANGE)));
        Assert.Equal(1, store.SavePendingCalls);
    }

    [Fact]
    public async Task RequestEmailChangeRejectsAWrongPassword()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.RequestEmailChangeAsync(
                _userId,
                "nueva@example.com",
                "incorrecta",
                "123456",
                _now));

        Assert.Equal(AccountEmailError.InvalidPassword, error.Error);
        Assert.Empty(store.Pending);
    }

    [Fact]
    public async Task RequestEmailChangeRejectsAnAddressAlreadyInUse()
    {
        var store = StoreWithAccount();
        store.EmailOwners["nueva@example.com"] = _otherUserId;
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.RequestEmailChangeAsync(
                _userId,
                "nueva@example.com",
                "Secreta123!",
                "123456",
                _now));

        Assert.Equal(AccountEmailError.EmailAlreadyInUse, error.Error);
        Assert.Empty(store.Pending);
    }

    [Fact]
    public async Task RequestEmailChangeRejectsTheCurrentAddress()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.RequestEmailChangeAsync(
                _userId,
                " ANA@Example.com ",
                "Secreta123!",
                "123456",
                _now));

        Assert.Equal(AccountEmailError.EmailUnchanged, error.Error);
    }

    [Fact]
    public async Task RequestEmailChangeRejectsAnInvalidAddress()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.RequestEmailChangeAsync(
                _userId,
                "ana.example.com",
                "Secreta123!",
                "123456",
                _now));

        Assert.Equal(AccountEmailError.InvalidEmail, error.Error);
    }

    [Fact]
    public async Task ConfirmEmailChangeRejectsAnInvalidCodeAndKeepsTheAccountUntouched()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestEmailChangeAsync(
            _userId,
            "nueva@example.com",
            "Secreta123!",
            "123456",
            _now);
        var session = Session(_userId, _now.AddHours(1).UtcDateTime);
        store.Sessions.Add(session);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmEmailChangeAsync(_userId, "000000", _now.AddMinutes(1)));

        Assert.Equal(AccountEmailError.InvalidCode, error.Error);
        Assert.Equal("ana@example.com", store.Accounts[_userId].Email);
        Assert.Equal(_now.AddDays(-1).UtcDateTime, store.Accounts[_userId].EmailVerifiedAt);
        Assert.Null(session.RevokedAt);
        Assert.Equal(0, store.SaveCalls);
        Assert.Equal(0, store.ListSessionsCalls);
    }

    [Fact]
    public async Task ConfirmEmailChangeAppliesTheNewAddressAndRevokesEveryActiveSession()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestEmailChangeAsync(
            _userId,
            "nueva@example.com",
            "Secreta123!",
            "123456",
            _now);
        var active = Session(_userId, _now.AddHours(1).UtcDateTime);
        var expired = Session(_userId, _now.AddMinutes(-1).UtcDateTime);
        var alreadyRevoked = Session(_userId, _now.AddHours(1).UtcDateTime);
        alreadyRevoked.Revoke(_now.AddDays(-1).UtcDateTime);
        var otherUser = Session(_otherUserId, _now.AddHours(1).UtcDateTime);
        store.Sessions.AddRange(new[] { active, expired, alreadyRevoked, otherUser });

        var result = await service.ConfirmEmailChangeAsync(
            _userId,
            "123456",
            _now.AddMinutes(1));

        Assert.Equal("nueva@example.com", result.Email);
        Assert.Equal(_now.AddMinutes(1).UtcDateTime, result.EmailVerifiedAt);
        Assert.Equal(new[] { active.Id }, result.SessionIdsToRevoke);
        Assert.Equal(_now.AddMinutes(1).UtcDateTime, active.RevokedAt);
        Assert.Null(expired.RevokedAt);
        Assert.Equal(_now.AddDays(-1).UtcDateTime, alreadyRevoked.RevokedAt);
        Assert.Null(otherUser.RevokedAt);
        Assert.False(store.Pending.ContainsKey((_userId, EmailVerificationPurpose.EMAIL_CHANGE)));
        Assert.Equal(1, store.SaveCalls);
        Assert.Equal(1, store.RemovePendingCalls);
    }

    [Fact]
    public async Task ConfirmEmailChangeRejectsAnAddressTakenAfterTheRequest()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestEmailChangeAsync(
            _userId,
            "nueva@example.com",
            "Secreta123!",
            "123456",
            _now);
        store.EmailOwners["nueva@example.com"] = _otherUserId;
        var session = Session(_userId, _now.AddHours(1).UtcDateTime);
        store.Sessions.Add(session);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmEmailChangeAsync(_userId, "123456", _now.AddMinutes(1)));

        Assert.Equal(AccountEmailError.EmailAlreadyInUse, error.Error);
        Assert.Equal("ana@example.com", store.Accounts[_userId].Email);
        Assert.Null(session.RevokedAt);
        Assert.Equal(0, store.SaveCalls);
    }

    [Fact]
    public async Task ConfirmEmailChangeWithoutAPendingRequestIsRejected()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmEmailChangeAsync(_userId, "123456", _now));

        Assert.Equal(AccountEmailError.VerificationNotRequested, error.Error);
    }

    [Fact]
    public async Task ConfirmEmailChangeRejectsAnExpiredCode()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestEmailChangeAsync(
            _userId,
            "nueva@example.com",
            "Secreta123!",
            "123456",
            _now.AddMinutes(-11));

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmEmailChangeAsync(_userId, "123456", _now));

        Assert.Equal(AccountEmailError.VerificationExpired, error.Error);
        Assert.Equal("ana@example.com", store.Accounts[_userId].Email);
    }

    [Fact]
    public async Task ConfirmEmailChangeStopsAfterFiveWrongAttempts()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestEmailChangeAsync(
            _userId,
            "nueva@example.com",
            "Secreta123!",
            "123456",
            _now);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var error = await Assert.ThrowsAsync<AccountEmailException>(
                () => service.ConfirmEmailChangeAsync(_userId, "000000", _now.AddMinutes(1)));
            Assert.Equal(AccountEmailError.InvalidCode, error.Error);
        }

        var exhausted = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmEmailChangeAsync(_userId, "123456", _now.AddMinutes(1)));

        Assert.Equal(AccountEmailError.TooManyAttempts, exhausted.Error);
        Assert.Equal("ana@example.com", store.Accounts[_userId].Email);
    }

    [Fact]
    public async Task RequestInstitutionalEmailRejectsANonInstitutionalDomain()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.RequestInstitutionalEmailAsync(
                _userId,
                "ana.perez@example.com",
                "123456",
                _now));

        Assert.Equal(AccountEmailError.InstitutionalDomainRequired, error.Error);
        Assert.Empty(store.Pending);
    }

    [Fact]
    public async Task RequestInstitutionalEmailStoresAPendingVerification()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var result = await service.RequestInstitutionalEmailAsync(
            _userId,
            " Ana.Perez@UCB.edu.bo ",
            "123456",
            _now);

        Assert.Equal("ana.perez@ucb.edu.bo", result.Email);
        Assert.Equal(EmailVerificationPurpose.INSTITUTIONAL_EMAIL, result.Purpose);
        Assert.True(store.Pending.ContainsKey((_userId, EmailVerificationPurpose.INSTITUTIONAL_EMAIL)));
    }

    [Fact]
    public async Task RequestInstitutionalEmailRejectsAnAddressAlreadyInUse()
    {
        var store = StoreWithAccount();
        store.EmailOwners["ana.perez@ucb.edu.bo"] = _otherUserId;
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.RequestInstitutionalEmailAsync(
                _userId,
                "ana.perez@ucb.edu.bo",
                "123456",
                _now));

        Assert.Equal(AccountEmailError.EmailAlreadyInUse, error.Error);
    }

    [Fact]
    public async Task ConfirmInstitutionalEmailMarksTheAddressAsVerifiedWithoutRevokingSessions()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestInstitutionalEmailAsync(
            _userId,
            "ana.perez@ucb.edu.bo",
            "123456",
            _now);
        var session = Session(_userId, _now.AddHours(1).UtcDateTime);
        store.Sessions.Add(session);

        var result = await service.ConfirmInstitutionalEmailAsync(
            _userId,
            "123456",
            _now.AddMinutes(1));

        Assert.Equal("ana.perez@ucb.edu.bo", result.InstitutionalEmail);
        Assert.Equal(_now.AddMinutes(1).UtcDateTime, result.InstitutionalEmailVerifiedAt);
        Assert.True(store.Accounts[_userId].HasVerifiedInstitutionalEmail);
        Assert.True(store.Accounts[_userId].CanHaveHourCommitments);
        Assert.Null(session.RevokedAt);
        Assert.Equal(0, store.ListSessionsCalls);
        Assert.False(store.Pending.ContainsKey((_userId, EmailVerificationPurpose.INSTITUTIONAL_EMAIL)));
    }

    [Fact]
    public async Task ConfirmInstitutionalEmailRejectsAnInvalidCode()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestInstitutionalEmailAsync(
            _userId,
            "ana.perez@ucb.edu.bo",
            "123456",
            _now);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmInstitutionalEmailAsync(_userId, "000000", _now.AddMinutes(1)));

        Assert.Equal(AccountEmailError.InvalidCode, error.Error);
        Assert.False(store.Accounts[_userId].HasVerifiedInstitutionalEmail);
        Assert.Null(store.Accounts[_userId].InstitutionalEmail);
    }

    [Fact]
    public async Task ConfirmInstitutionalEmailRejectsAnAddressTakenAfterTheRequest()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestInstitutionalEmailAsync(
            _userId,
            "ana.perez@ucb.edu.bo",
            "123456",
            _now);
        store.EmailOwners["ana.perez@ucb.edu.bo"] = _otherUserId;

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmInstitutionalEmailAsync(_userId, "123456", _now.AddMinutes(1)));

        Assert.Equal(AccountEmailError.EmailAlreadyInUse, error.Error);
        Assert.False(store.Accounts[_userId].HasVerifiedInstitutionalEmail);
    }

    [Fact]
    public async Task ConfirmInstitutionalEmailWithoutAPendingRequestIsRejected()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmInstitutionalEmailAsync(_userId, "123456", _now));

        Assert.Equal(AccountEmailError.VerificationNotRequested, error.Error);
    }

    [Fact]
    public async Task PendingInstitutionalVerificationCannotBeUsedToChangeTheAccessEmail()
    {
        var store = StoreWithAccount();
        var service = new AccountEmailService(store);
        await service.RequestInstitutionalEmailAsync(
            _userId,
            "ana.perez@ucb.edu.bo",
            "123456",
            _now);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmEmailChangeAsync(_userId, "123456", _now.AddMinutes(1)));

        Assert.Equal(AccountEmailError.VerificationNotRequested, error.Error);
        Assert.Equal("ana@example.com", store.Accounts[_userId].Email);
    }

    [Fact]
    public async Task RequestEmailChangeRejectsAnUnknownAccount()
    {
        var store = new FakeAccountEmailStore();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.RequestEmailChangeAsync(
                _userId,
                "nueva@example.com",
                "Secreta123!",
                "123456",
                _now));

        Assert.Equal(AccountEmailError.AccountNotFound, error.Error);
    }

    [Fact]
    public async Task ConfirmEmailChangeRejectsAnUnknownAccount()
    {
        var store = new FakeAccountEmailStore();
        var service = new AccountEmailService(store);

        var error = await Assert.ThrowsAsync<AccountEmailException>(
            () => service.ConfirmEmailChangeAsync(_userId, "123456", _now));

        Assert.Equal(AccountEmailError.AccountNotFound, error.Error);
    }

    private static FakeAccountEmailStore StoreWithAccount(string email = "ana@example.com")
    {
        var store = new FakeAccountEmailStore();
        store.Accounts[_userId] = new AccountEmail
        {
            UserId = _userId,
            Email = email,
            EmailVerifiedAt = _now.AddDays(-1).UtcDateTime,
        };
        return store;
    }

    private static AuthSession Session(Guid userId, DateTime expiresAt) =>
        new() { UserId = userId, ExpiresAt = expiresAt };
}
