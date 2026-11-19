using System.Text.Json.Nodes;
using Uvoluntapp.Backend.Domain.Audit;

namespace Uvoluntapp.Backend.Application.Audit;

public sealed record AuditEventResponse(
    Guid Id,
    Guid? ActorId,
    AuditActorType ActorType,
    string Action,
    string EntityType,
    Guid EntityId,
    JsonNode? Details,
    DateTime CreatedAt);
