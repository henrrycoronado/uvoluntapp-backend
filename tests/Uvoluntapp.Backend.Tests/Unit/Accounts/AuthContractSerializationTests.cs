using System.Linq;
using System.Text.Json;
using System;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Tests;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Accounts;

public class AuthContractSerializationTests
{
    private const string PasswordField = "password";

    private static readonly JsonSerializerOptions _webOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void RegisterRequestUsesContractPropertyNames()
    {
        var request = new RegisterRequest
        {
            Email = "ana.perez@ucb.edu.bo",
            Password = TestCredentials.Valid,
            FirstName = "Ana",
            LastName = "Pérez",
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(request, _webOptions));

        Assert.Equal(
            new[] { "email", "firstName", "lastName", "password", "phone" },
            document.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
    }

    [Fact]
    public void RegisterResponseDeserializesFromContract()
    {
        const string Json = """{"message":"Si el correo es válido, se envió un código.","requiresOtp":true}""";

        var response = JsonSerializer.Deserialize<RegisterResponse>(Json, _webOptions);

        Assert.NotNull(response);
        Assert.True(response.RequiresOtp);
        Assert.Equal("Si el correo es válido, se envió un código.", response.Message);
    }

    [Fact]
    public void VerifyOtpResponseDeserializesTokens()
    {
        const string Json = """
            {
              "accountStatus": "ACTIVE",
              "requiresApprovalData": false,
              "tokens": {
                "accessToken": "access",
                "refreshToken": "refresh",
                "expiresAt": "2026-11-19T12:00:00Z"
              }
            }
            """;

        var response = JsonSerializer.Deserialize<VerifyOtpResponse>(Json, _webOptions);

        Assert.NotNull(response);
        Assert.Equal("ACTIVE", response.AccountStatus);
        Assert.False(response.RequiresApprovalData);
        Assert.NotNull(response.Tokens);
        Assert.Equal("access", response.Tokens.AccessToken);
        Assert.Equal("refresh", response.Tokens.RefreshToken);
        Assert.Equal(DateTimeOffset.Parse("2026-11-19T12:00:00Z"), response.Tokens.ExpiresAt);
    }

    [Fact]
    public void TokenResponseDeserializesFromContract()
    {
        const string Json = """
            {"accessToken":"access","refreshToken":"refresh","expiresAt":"2026-11-19T12:00:00Z"}
            """;

        var response = JsonSerializer.Deserialize<TokenResponse>(Json, _webOptions);

        Assert.NotNull(response);
        Assert.Equal("access", response.AccessToken);
        Assert.Equal(DateTimeOffset.Parse("2026-11-19T12:00:00Z"), response.ExpiresAt);
    }

    [Fact]
    public void RequestsDeserializeFromCamelCase()
    {
        var loginJson = $$"""{"email":"ana.perez@ucb.edu.bo","{{PasswordField}}":"{{TestCredentials.Valid}}"}""";
        const string CompleteJson = """
            {
              "email": "ana.perez@gmail.com",
              "firstName": "Ana",
              "lastName": "Pérez",
              "phone": "+591 70000000",
              "relationWithPastoral": "Voluntaria externa"
            }
            """;

        var login = JsonSerializer.Deserialize<LoginRequest>(loginJson, _webOptions);
        var complete = JsonSerializer.Deserialize<CompleteRegistrationRequest>(CompleteJson, _webOptions);

        Assert.NotNull(login);
        Assert.Equal("ana.perez@ucb.edu.bo", login.Email);
        Assert.NotNull(complete);
        Assert.Equal("+591 70000000", complete.Phone);
        Assert.Equal("Voluntaria externa", complete.RelationWithPastoral);
    }
}
