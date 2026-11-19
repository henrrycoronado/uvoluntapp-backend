using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Uvoluntapp.Backend.Infrastructure.Persistence;
namespace Uvoluntapp.Backend.Tests.Integration;

[Collection(nameof(HostConnectionTests))]
public sealed class HostConnectionTests : IDisposable
{
    private const string DefaultKey = "ConnectionStrings__DefaultConnection";
    private const string EnvironmentKey = "DB_CONNECTION_STRING";
    private const string DefaultConnection = "Host=default.invalid;Database=config_test";
    private const string EnvironmentConnection = "Host=environment.invalid;Database=config_test";
    private readonly string? _originalDefault = Environment.GetEnvironmentVariable(DefaultKey);
    private readonly string? _originalEnvironment = Environment.GetEnvironmentVariable(EnvironmentKey);

    [Theory]
    [InlineData(null, EnvironmentConnection, EnvironmentConnection)]
    [InlineData(DefaultConnection, null, DefaultConnection)]
    [InlineData(DefaultConnection, EnvironmentConnection, DefaultConnection)]
    [InlineData(" ", EnvironmentConnection, EnvironmentConnection)]
    public void HostResolvesConfiguredConnection(string? defaultValue, string? environmentValue, string expected)
    {
        SetConnections(defaultValue, environmentValue);
        using var host = new WebApplicationFactory<Program>();
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(expected, context.Database.GetConnectionString());
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", " ")]
    public void HostFailsToStartWithoutAConnection(string? defaultValue, string? environmentValue)
    {
        SetConnections(defaultValue, environmentValue);
        using var host = new WebApplicationFactory<Program>();

        var error = Assert.Throws<InvalidOperationException>(() => host.Services);
        Assert.Contains("ConnectionStrings:DefaultConnection", error.Message);
        Assert.Contains(EnvironmentKey, error.Message);
    }

    public void Dispose() => SetConnections(_originalDefault, _originalEnvironment);

    private static void SetConnections(string? defaultValue, string? environmentValue)
    {
        Environment.SetEnvironmentVariable(DefaultKey, defaultValue);
        Environment.SetEnvironmentVariable(EnvironmentKey, environmentValue);
        Environment.SetEnvironmentVariable("DATABASE_AUTO_MIGRATE", "false");
    }
}
