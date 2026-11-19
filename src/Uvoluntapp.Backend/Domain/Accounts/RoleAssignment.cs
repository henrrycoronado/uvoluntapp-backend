namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed class RoleAssignment : Entity
{
    public Guid UserId { get; set; }

    public required AssignedRole Role { get; set; }

    public RoleScope Scope { get; set; } = RoleScope.GLOBAL;

    public Guid? ScopeId { get; set; }

    public Guid GrantedBy { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActiveAt(DateTime instant)
    {
        return RevokedAt is null
            && (ValidFrom is null || ValidFrom <= instant)
            && (ValidUntil is null || ValidUntil > instant);
    }
}
