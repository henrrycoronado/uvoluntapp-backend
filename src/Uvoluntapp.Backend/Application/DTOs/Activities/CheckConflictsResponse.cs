namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record CheckConflictsResponse(bool HasConflicts, IReadOnlyList<ConflictWarning> Conflicts);
