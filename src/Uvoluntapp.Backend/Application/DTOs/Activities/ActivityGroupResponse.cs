using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record ActivityGroupResponse(Guid Id, string Name, DateTime StartsAt, DateTime EndsAt, int? Capacity)
{
    public static ActivityGroupResponse From(ActivityGroup group) => new(group.Id, group.Name, group.StartsAt, group.EndsAt, group.Capacity);
}
