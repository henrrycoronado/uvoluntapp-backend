using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.DTOs.Programs;

/// <summary>Crear o editar la ficha de un programa (SCRUM-45).</summary>
public sealed record ProgramDetailsRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [MaxLength(300)]
    public string? Contact { get; init; }

    [MaxLength(500)]
    public string? ReferenceSchedule { get; init; }
}
