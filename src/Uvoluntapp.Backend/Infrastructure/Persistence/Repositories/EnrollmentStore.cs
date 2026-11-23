using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Domain.Entities.Activities;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class EnrollmentStore(AppDbContext db) : IEnrollmentStore
{
    // Orden fijo. La fila de usuario serializa inscripciones cruzadas, la de actividad protege el último cupo.
    public async Task<EnrollmentResponse> WithLocksAsync(Guid userId, Guid activityId, Func<Task<EnrollmentResponse>> operation, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM users WHERE id = {userId} FOR UPDATE", ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM activities WHERE id = {activityId} FOR UPDATE", ct);
        var response = await operation();
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return response;
    }

    public Task<List<Enrollment>> ListAsync(Guid? userId, Guid? activityId, CancellationToken ct) =>
        db.Enrollments.Where(e => (!userId.HasValue || e.UserId == userId) && (!activityId.HasValue || e.ActivityId == activityId)).OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

    public Task<int> OccupancyAsync(Guid activityId, Guid? groupId, CancellationToken ct) =>
        db.Enrollments.CountAsync(e => e.ActivityId == activityId && e.GroupId == groupId && e.CancelledAt == null, ct);

    public Task<bool> OverlapsAsync(Guid userId, DateTime start, DateTime end, CancellationToken ct) =>
        (from e in db.Enrollments
         join a in db.Activities on e.ActivityId equals a.Id
         join g in db.Set<ActivityGroup>() on e.GroupId equals g.Id into groups
         from g in groups.DefaultIfEmpty()
         where e.UserId == userId && e.CancelledAt == null && a.Status != ActivityStatus.CANCELLED
            && (g == null ? a.StartsAt : g.StartsAt) < end && (g == null ? a.EndsAt : g.EndsAt) > start
         select e.Id).AnyAsync(ct);

    public Task<List<string>> RecipientsAsync(Guid activityId, CancellationToken ct) =>
        (from e in db.Enrollments
         join u in db.Users on e.UserId equals u.Id
         where e.ActivityId == activityId && e.CancelledAt == null && u.EmailConfirmed && u.Email != null
         select u.Email!).Distinct().ToListAsync(ct);

    public void Add(Enrollment enrollment) => db.Enrollments.Add(enrollment);
}
