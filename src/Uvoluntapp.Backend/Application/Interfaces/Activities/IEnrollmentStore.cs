using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.Interfaces.Activities;

public interface IEnrollmentStore
{
    Task<EnrollmentResponse> WithLocksAsync(Guid userId, Guid activityId, Func<Task<EnrollmentResponse>> operation, CancellationToken ct);

    Task<List<Enrollment>> ListAsync(Guid? userId, Guid? activityId, CancellationToken ct);

    Task<Dictionary<Guid, (string Name, string? Email)>> ParticipantsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct);

    Task<int> OccupancyAsync(Guid activityId, Guid? groupId, CancellationToken ct);

    Task<bool> OverlapsAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct);

    Task<List<string>> RecipientsAsync(Guid activityId, CancellationToken ct);

    void Add(Enrollment enrollment);
}
