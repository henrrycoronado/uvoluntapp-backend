using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using U_VoluntApp_Core.Src.Domain.Entities;
using Xunit;

namespace U_VoluntApp_Core.Tests.Integration;

public sealed class CareerPersistenceTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task CareerCanBeInsertedReadAndUpdated()
    {
        var id = await InsertCareerAsync();
        await using var context = fixture.CreateContext();
        var career = await context.Careers.SingleAsync(c => c.Id == id);
        Assert.Equal("Medicina", career.Name);
        var originalVersion = context.Entry(career).Property<uint>("xmin").CurrentValue;

        career.Name = "Enfermería";
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var updated = await context.Careers.SingleAsync(c => c.Id == id);
        Assert.Equal("Enfermería", updated.Name);
        Assert.NotEqual(originalVersion, context.Entry(updated).Property<uint>("xmin").CurrentValue);
    }

    [Fact]
    public async Task StaleCareerUpdateThrowsConcurrencyException()
    {
        var id = await InsertCareerAsync();
        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var current = await first.Careers.SingleAsync(c => c.Id == id);
        var stale = await second.Careers.SingleAsync(c => c.Id == id);

        current.Name = "Derecho";
        await first.SaveChangesAsync();

        stale.Name = "Economía";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await second.Entry(stale).ReloadAsync();
        Assert.Equal("Derecho", stale.Name);
    }

    private async Task<Guid> InsertCareerAsync()
    {
        await using var context = fixture.CreateContext();
        var career = new Career { Name = "Medicina" };
        context.Careers.Add(career);
        await context.SaveChangesAsync();
        return career.Id;
    }
}
