using System.Net.Http;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Infrastructure.Persistence;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration.Accounts;

public sealed class AuthFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgres;
    private IContainer? _redis;
    private WebApplicationFactory<Program> _host = null!;
    private string? _composeAdmin;
    private readonly string _database = "m1a_" + Guid.NewGuid().ToString("N");

    public TestMailbox Mail { get; } = new();

    public string ConnectionString { get; private set; } = string.Empty;

    public string RedisConnection { get; private set; } = string.Empty;

    public IServiceProvider Services => _host.Services;

    public IConnectionMultiplexer Redis => _host.Services.GetRequiredService<IConnectionMultiplexer>();

    public HttpClient Client()
    {
        var client = _host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost/api/v1/auth/"),
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Test-Peer", new IPAddress(Guid.NewGuid().ToByteArray()).ToString());
        return client;
    }

    public AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        _composeAdmin = Environment.GetEnvironmentVariable("M1_TEST_POSTGRES");
        var redisConnection = Environment.GetEnvironmentVariable("M1_TEST_REDIS");
        if (_composeAdmin is null && redisConnection is null)
        {
            _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();
            _redis = new ContainerBuilder("redis:7-alpine")
                .WithPortBinding(6379, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("redis-cli", "ping"))
                .Build();
            await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());
            ConnectionString = _postgres.GetConnectionString();
            redisConnection = $"{_redis.Hostname}:{_redis.GetMappedPublicPort(6379)}";
        }
        else if (_composeAdmin is not null && redisConnection is not null)
        {
            await using var connection = new NpgsqlConnection(_composeAdmin);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE {_database}", connection);
            await command.ExecuteNonQueryAsync();
            ConnectionString = new NpgsqlConnectionStringBuilder(_composeAdmin) { Database = _database }.ConnectionString;
        }
        else
        {
            throw new InvalidOperationException("Configura M1_TEST_POSTGRES y M1_TEST_REDIS juntos.");
        }

        RedisConnection = redisConnection!;
        await using var db = Context();
        await db.Database.MigrateAsync();
        _host = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
            builder.UseSetting("JWT_SECRET", Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
            builder.UseSetting("JWT_ISSUER", "auth-test");
            builder.UseSetting("JWT_AUDIENCE", "auth-test-client");
            builder.UseSetting("REDIS_CONNECTION_STRING", redisConnection);
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IEmailSender>(Mail);
                services.AddSingleton<IStartupFilter, TestPeerFilter>();
            });
        });
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }

        if (_redis is not null)
        {
            await _redis.DisposeAsync();
        }

        if (_composeAdmin is not null && ConnectionString.Length > 0)
        {
            await using var connection = new NpgsqlConnection(_composeAdmin);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP DATABASE {_database} WITH (FORCE)", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
