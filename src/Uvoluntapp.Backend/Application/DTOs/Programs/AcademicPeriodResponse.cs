using Uvoluntapp.Backend.Domain.Entities.Programs;

namespace Uvoluntapp.Backend.Application.DTOs.Programs;

public sealed record AcademicPeriodResponse(Guid Id, string Label, DateTime StartDate, DateTime? EndDate, DateTime CreatedAt, string Status)
{
    public static AcademicPeriodResponse From(AcademicPeriod period) =>
        new(period.Id, period.Label, period.StartsAt, period.EndsAt, period.CreatedAt, period.IsActive ? "ACTIVE" : "CLOSED");
}
