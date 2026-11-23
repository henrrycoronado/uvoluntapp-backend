using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Application.DTOs.Activities;

public sealed record EnrollmentResponse(Guid Id, Guid ActivityId, Guid? GroupId, Guid UserId, string Status, DateTime CreatedAt, DateTime? CancelledAt, string? ParticipantName = null, string? ParticipantEmail = null)
{
    // Solo lo ve quien gestiona la actividad: la persona que se inscribe no necesita sus propios datos en la respuesta.
    public EnrollmentResponse WithParticipant(string? name, string? email) => this with { ParticipantName = name, ParticipantEmail = email };

    public static EnrollmentResponse From(Enrollment e) => new(e.Id, e.ActivityId, e.GroupId, e.UserId, e.CancelledAt.HasValue ? "CANCELLED" : "ENROLLED", e.CreatedAt, e.CancelledAt);
}
