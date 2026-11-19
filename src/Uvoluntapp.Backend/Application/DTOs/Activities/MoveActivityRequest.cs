using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

/// <summary>Reasignar una actividad a otro programa o volverla libre (SCRUM-123). Sin programa = libre.</summary>
public sealed record MoveActivityRequest
{
    public Guid? ProgramId { get; init; }

    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}
