using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

/// <summary>
/// Filtro de actividades. <see cref="DraftProgramIds"/> y <see cref="DraftActivityIds"/> limitan los
/// borradores visibles al ámbito del actor; con <see cref="AllDrafts"/> los ve todos (ADMIN).
/// </summary>
public sealed record ActivitySearch(
    Guid? ProgramId,
    Guid? PeriodId,
    IReadOnlyCollection<ActivityStatus>? Statuses,
    bool? Independent,
    string? GroupLabel,
    DateTime? From,
    DateTime? To,
    bool AllDrafts,
    IReadOnlyCollection<Guid> DraftProgramIds,
    IReadOnlyCollection<Guid> DraftActivityIds,
    bool OnlyActivePrograms,
    int Page,
    int PageSize);
