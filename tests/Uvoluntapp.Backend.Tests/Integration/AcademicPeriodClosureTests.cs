using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Integration;

public sealed class AcademicPeriodClosureTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task ClosingAndRetryingReturnsTheSameActiveSuccessor()
    {
        // La prueba usa una transacción de cierre real sobre un periodo sin actividades pendientes.
        await using var db = fixture.CreateContext();
        var predecessor = await db.AcademicPeriods.SingleAsync(p => p.IsActive);
        var today = DateTime.UtcNow.Date;
        var store = new Uvoluntapp.Backend.Infrastructure.Persistence.Repositories.AcademicPeriodStore(db);
        await store.ChangeAsync(predecessor.Id, predecessor.Label, today, today, false, _ => Task.CompletedTask, default);
        var label = Guid.NewGuid().ToString("N")[..12];
        var next = await store.ChangeAsync(predecessor.Id, label, null, today, true, _ => Task.CompletedTask, default);
        var retry = await store.ChangeAsync(predecessor.Id, label, null, today, true, _ => Task.CompletedTask, default);
        Assert.Equal(next.Id, retry.Id);
        Assert.Equal(next.Id, (await db.AcademicPeriods.SingleAsync(p => p.IsActive)).Id);
        Assert.Null(next.EndsAt);
    }
}
