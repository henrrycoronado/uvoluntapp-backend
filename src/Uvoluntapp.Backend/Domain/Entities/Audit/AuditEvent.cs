using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Audit;

public sealed class AuditEvent : Entity
{
    public Guid? ActorId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public string Data { get; set; } = "{}";

    public static AuditEvent Create(
        Guid actorId,
        string action,
        string resourceType,
        Guid resourceId,
        IReadOnlyDictionary<string, object?>? details = null,
        DateTime? occurredAt = null)
    {
        if (actorId == Guid.Empty)
        {
            throw new ArgumentException("El actor es obligatorio.", nameof(actorId));
        }

        return Build(actorId, action, resourceType, resourceId, details, occurredAt);
    }

    public static AuditEvent CreateSystem(
        string action,
        string resourceType,
        Guid resourceId,
        IReadOnlyDictionary<string, object?>? details = null,
        DateTime? occurredAt = null) =>
        Build(null, action, resourceType, resourceId, details, occurredAt);

    private static AuditEvent Build(
        Guid? actorId,
        string action,
        string resourceType,
        Guid resourceId,
        IReadOnlyDictionary<string, object?>? details,
        DateTime? occurredAt)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("La acción es obligatoria.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ArgumentException("El tipo de recurso es obligatorio.", nameof(resourceType));
        }

        if (resourceId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del recurso es obligatorio.", nameof(resourceId));
        }

        return new AuditEvent
        {
            ActorId = actorId,
            Action = action.Trim(),
            ResourceType = resourceType.Trim(),
            ResourceId = resourceId,
            Data = AuditDetails.Serialize(details),
            CreatedAt = NormalizeUtc(occurredAt),
        };
    }

    private static DateTime NormalizeUtc(DateTime? occurredAt)
    {
        var moment = occurredAt ?? DateTime.UtcNow;
        return moment.Kind switch
        {
            DateTimeKind.Utc => moment,
            DateTimeKind.Local => moment.ToUniversalTime(),
            _ => DateTime.SpecifyKind(moment, DateTimeKind.Utc),
        };
    }
}
