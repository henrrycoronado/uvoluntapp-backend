using System;
using System.Collections.Generic;
using Uvoluntapp.Backend.Application.Accounts.Approvals;
using Uvoluntapp.Backend.Application.Profiles;
using Uvoluntapp.Backend.Domain.Accounts;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit;

public sealed class ProfileAndApprovalTests
{
    [Fact]
    public void OwnerCanUpdateAllowedProfileFields()
    {
        var userId = Guid.NewGuid();
        var profile = new Profile
        {
            UserId = userId,
            FirstName = "Ana",
            LastName = "Paz",
            Phone = "70000000",
        };
        var service = new ProfileService();

        var result = service.UpdateMyProfile(
            userId,
            profile,
            new UpdateProfileRequest(FirstName: "  María  ", Phone: "71111111"),
            new HashSet<ProfileUpdateField>
            {
                ProfileUpdateField.FIRST_NAME,
                ProfileUpdateField.PHONE,
            });

        Assert.Equal("María", result.FirstName);
        Assert.Equal("71111111", result.Phone);
        Assert.Equal("Paz", result.LastName);
    }

    [Fact]
    public void ThirdPartyCannotUpdateProfile()
    {
        var profile = new Profile { UserId = Guid.NewGuid() };

        Assert.Throws<UnauthorizedAccessException>(() =>
            new ProfileService().UpdateMyProfile(
                Guid.NewGuid(),
                profile,
                new UpdateProfileRequest(FirstName: "Intruso"),
                new HashSet<ProfileUpdateField> { ProfileUpdateField.FIRST_NAME }));
    }

    [Fact]
    public void ProfileRejectsFieldNotAllowedByRole()
    {
        var userId = Guid.NewGuid();
        var profile = new Profile { UserId = userId };

        Assert.Throws<InvalidOperationException>(() =>
            new ProfileService().UpdateMyProfile(
                userId,
                profile,
                new UpdateProfileRequest(Phone: "71111111"),
                new HashSet<ProfileUpdateField> { ProfileUpdateField.FIRST_NAME }));
    }

    [Fact]
    public void ProfileNeverUpdatesEmail()
    {
        var userId = Guid.NewGuid();
        var profile = new Profile { UserId = userId };

        Assert.Throws<ArgumentException>(() =>
            new ProfileService().UpdateMyProfile(
                userId,
                profile,
                new UpdateProfileRequest(Email: "new@example.com"),
                new HashSet<ProfileUpdateField> { ProfileUpdateField.EMAIL }));
    }

    [Fact]
    public void OnlyActiveGlobalAdminCanApprove()
    {
        var actorId = Guid.NewGuid();
        var request = new AccountApprovalRequest();
        var service = new AccountApprovalService();

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.Approve(
                actorId,
                request,
                new[]
                {
                    new RoleAssignment
                    {
                        UserId = actorId,
                        Role = AssignedRole.PROGRAM_LEADER,
                        Scope = RoleScope.GLOBAL,
                    },
                },
                new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void RejectionRequiresReason()
    {
        var actorId = Guid.NewGuid();
        var service = new AccountApprovalService();
        var request = new AccountApprovalRequest();
        var admin = AdminRole(actorId);

        Assert.Throws<ArgumentException>(() =>
            service.Reject(
                actorId,
                request,
                new[] { admin },
                "  ",
                new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void RejectedRequestCanBeResubmittedTheFollowingDayAndHistoryRemains()
    {
        var actorId = Guid.NewGuid();
        var rejectedAt = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        var service = new AccountApprovalService();
        var rejected = new AccountApprovalRequest
        {
            UserId = actorId,
            FirstName = "Ana",
            LastName = "Paz",
            Relationship = "Voluntaria",
        };
        service.Reject(actorId, rejected, new[] { AdminRole(actorId) }, "Datos incompletos", rejectedAt);

        Assert.Throws<InvalidOperationException>(() =>
            service.CreateRequest(
                actorId,
                new[] { rejected },
                "Ana",
                "Paz",
                null,
                "Voluntaria",
                rejectedAt.AddHours(23)));

        var resubmission = service.CreateRequest(
            actorId,
            new[] { rejected },
            "Ana",
            "Paz",
            null,
            "Voluntaria",
            rejectedAt.AddDays(1));

        Assert.Equal(AccountApprovalStatus.REJECTED, rejected.Status);
        Assert.Equal(AccountApprovalStatus.PENDING_APPROVAL, resubmission.Status);
        Assert.NotEqual(rejected.Id, resubmission.Id);
    }

    private static RoleAssignment AdminRole(Guid userId) =>
        new()
        {
            UserId = userId,
            Role = AssignedRole.ADMIN,
            Scope = RoleScope.GLOBAL,
        };
}
