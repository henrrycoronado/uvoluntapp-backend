namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record GroupRequest(string Name, DateTimeOffset StartsAt, DateTimeOffset EndsAt, int? Capacity);
