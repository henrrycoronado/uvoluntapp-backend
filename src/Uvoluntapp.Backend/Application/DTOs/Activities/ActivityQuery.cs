using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

/// <summary>Filtros de la consulta de actividades (lista y calendario comparten la fuente, SCRUM-52).</summary>
public sealed record ActivityQuery(
    Guid? ProgramId,
    Guid? PeriodId,
    ActivityStatus? Status,
    bool? Independent,
    string? GroupLabel,
    DateTime? From,
    DateTime? To,
    int Page,
    int PageSize);
