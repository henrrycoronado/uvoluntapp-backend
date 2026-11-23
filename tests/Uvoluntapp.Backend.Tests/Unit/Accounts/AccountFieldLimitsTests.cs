using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class AccountFieldLimitsTests
{
    private static string Long(int length) => new('a', length);

    [Fact]
    public void RegisterAcceptsValuesAtTheLimit()
    {
        var request = new RegisterRequest
        {
            Email = Long(FieldLimits.Email - "@ucb.edu.bo".Length) + "@ucb.edu.bo",
            Password = Long(FieldLimits.Password),
            FirstName = Long(FieldLimits.PersonName),
            LastName = Long(FieldLimits.PersonName),
            Phone = Long(FieldLimits.Phone),
        };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(nameof(RegisterRequest.FirstName))]
    [InlineData(nameof(RegisterRequest.LastName))]
    [InlineData(nameof(RegisterRequest.Password))]
    [InlineData(nameof(RegisterRequest.Phone))]
    [InlineData(nameof(RegisterRequest.Email))]
    public void RegisterRejectsValuesOverTheLimit(string member)
    {
        var request = member switch
        {
            nameof(RegisterRequest.FirstName) => ValidRegister() with { FirstName = Long(FieldLimits.PersonName + 1) },
            nameof(RegisterRequest.LastName) => ValidRegister() with { LastName = Long(FieldLimits.PersonName + 1) },
            nameof(RegisterRequest.Password) => ValidRegister() with { Password = Long(FieldLimits.Password + 1) },
            nameof(RegisterRequest.Phone) => ValidRegister() with { Phone = Long(FieldLimits.Phone + 1) },
            _ => ValidRegister() with { Email = Long(FieldLimits.Email) + "@ucb.edu.bo" },
        };

        Assert.Contains(Validate(request), e => e.MemberNames.Contains(member));
    }

    [Fact]
    public void LoginRejectsOversizedPassword()
    {
        var request = new LoginRequest { Email = "ana@ucb.edu.bo", Password = Long(FieldLimits.Password + 1) };

        Assert.Contains(Validate(request), e => e.MemberNames.Contains(nameof(LoginRequest.Password)));
    }

    [Fact]
    public void ReapplyAndCompleteRegistrationRejectOversizedRelationship()
    {
        var tooLong = Long(FieldLimits.Relationship + 1);

        Assert.Contains(
            Validate(new ReapplyRequest { Email = "a@b.co", Password = "x", FirstName = "A", LastName = "B", Phone = "7", RelationWithPastoral = tooLong }),
            e => e.MemberNames.Contains(nameof(ReapplyRequest.RelationWithPastoral)));
        Assert.Contains(
            Validate(new CompleteRegistrationRequest { Email = "a@b.co", FirstName = "A", LastName = "B", Phone = "7", RelationWithPastoral = tooLong }),
            e => e.MemberNames.Contains(nameof(CompleteRegistrationRequest.RelationWithPastoral)));
    }

    private static RegisterRequest ValidRegister() => new()
    {
        Email = "ana.perez@ucb.edu.bo",
        Password = TestCredentials.Valid,
        FirstName = "Ana",
        LastName = "Pérez",
        Phone = "70000000",
    };

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
