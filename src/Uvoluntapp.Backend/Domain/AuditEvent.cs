namespace Uvoluntapp.Backend.Domain;

public sealed class AuditEvent : Entity
{
    public Guid? ActorId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public string Data { get; set; } = "{}";
}
