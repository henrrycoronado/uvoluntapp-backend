using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record RescheduleActivityRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; init; } = string.Empty;

    public DateTime NewStartsAt { get; init; }

    public DateTime NewEndsAt { get; init; }
}
