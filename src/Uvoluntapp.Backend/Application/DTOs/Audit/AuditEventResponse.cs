using System.Text.Json.Nodes;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.DTOs.Audit;

public sealed record AuditEventResponse(
    Guid Id,
    Guid? ActorId,
    AuditActorType ActorType,
    string Action,
    string EntityType,
    Guid EntityId,
    JsonNode? Details,
    DateTime CreatedAt);
