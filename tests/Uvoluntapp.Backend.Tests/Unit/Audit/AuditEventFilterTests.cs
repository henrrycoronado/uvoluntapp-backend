using System;
using Uvoluntapp.Backend.Application.Audit;
using Xunit;

namespace Uvoluntapp.Backend.Tests.Unit.Audit;

public class AuditEventFilterTests
{
    [Fact]
    public void NormalizeAppliesContractDefaults()
    {
        var filter = new AuditEventFilter().Normalize();

        Assert.Equal(1, filter.Page);
        Assert.Equal(20, filter.PageSize);
        Assert.Null(filter.ActorId);
        Assert.Null(filter.Action);
        Assert.Null(filter.EntityType);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void NormalizeRejectsPaginationOutsideContract(int page, int pageSize)
    {
        var filter = new AuditEventFilter(Page: page, PageSize: pageSize);

        Assert.Throws<ArgumentOutOfRangeException>(filter.Normalize);
    }

    [Fact]
    public void NormalizeRejectsReversedDateRange()
    {
        var filter = new AuditEventFilter(
            From: new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 11, 19, 11, 0, 0, DateTimeKind.Utc));

        Assert.Throws<ArgumentException>(filter.Normalize);
    }

    [Fact]
    public void NormalizeTrimsFiltersAndConvertsDatesToUtc()
    {
        var localFrom = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Local);
        var filter = new AuditEventFilter(
            Action: "  ROLE_ASSIGNED ",
            EntityType: " ROLE_ASSIGNMENT ",
            From: localFrom).Normalize();

        Assert.Equal("ROLE_ASSIGNED", filter.Action);
        Assert.Equal("ROLE_ASSIGNMENT", filter.EntityType);
        Assert.Equal(DateTimeKind.Utc, filter.From!.Value.Kind);
        Assert.Equal(localFrom.ToUniversalTime(), filter.From);
    }

    [Fact]
    public void NormalizeTurnsBlankFiltersIntoNull()
    {
        var filter = new AuditEventFilter(Action: "   ", EntityType: string.Empty).Normalize();

        Assert.Null(filter.Action);
        Assert.Null(filter.EntityType);
    }

    [Fact]
    public void NormalizeRejectsEmptyIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => new AuditEventFilter(ActorId: Guid.Empty).Normalize());
        Assert.Throws<ArgumentException>(() => new AuditEventFilter(EntityId: Guid.Empty).Normalize());
    }
}
