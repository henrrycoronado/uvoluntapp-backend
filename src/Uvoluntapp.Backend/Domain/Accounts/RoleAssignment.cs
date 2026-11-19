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

    public static RoleAssignment Assign(
        Guid userId,
        AssignedRole role,
        RoleScope scope,
        Guid? scopeId,
        Guid grantedBy,
        DateTime? validFrom = null,
        DateTime? validUntil = null)
    {
        if (role == AssignedRole.ADMIN && scope != RoleScope.GLOBAL)
        {
            throw new ArgumentException("ADMIN requiere ámbito GLOBAL.", nameof(scope));
        }

        if (scope == RoleScope.GLOBAL)
        {
            if (scopeId.HasValue)
            {
                throw new ArgumentException("GLOBAL no puede tener scope_id.", nameof(scopeId));
            }
        }
        else if (!scopeId.HasValue)
        {
            throw new ArgumentException("Los ámbitos PROGRAM y ACTIVITY requieren scope_id.", nameof(scopeId));
        }

        if (validFrom.HasValue && validUntil.HasValue &&
            validUntil.Value < validFrom.Value)
        {
            throw new ArgumentException("validUntil no puede ser anterior a validFrom.", nameof(validUntil));
        }

        return new RoleAssignment
        {
            UserId = userId,
            Role = role,
            Scope = scope,
            ScopeId = scopeId,
            GrantedBy = grantedBy,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
        };
    }

    public void Revoke(DateTime? revokedAt = null)
    {
        if (RevokedAt.HasValue)
        {
            throw new InvalidOperationException("La asignación ya fue revocada.");
        }

        RevokedAt = revokedAt ?? DateTime.UtcNow;
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        return !RevokedAt.HasValue &&
            (!ValidFrom.HasValue || ValidFrom.Value <= utcNow) &&
            (!ValidUntil.HasValue || utcNow <= ValidUntil.Value);
    }

    public bool Covers(RoleScope resourceScope, Guid? resourceId, Guid? resourceProgramId)
    {
        // Un líder con ámbito GLOBAL no gestiona nada hasta que se le asigne un programa o actividad.
        if (Scope == RoleScope.GLOBAL)
        {
            return Role == AssignedRole.ADMIN;
        }

        if (Scope == RoleScope.ACTIVITY)
        {
            return resourceScope == RoleScope.ACTIVITY && ScopeId == resourceId;
        }

        if (Scope == RoleScope.PROGRAM)
        {
            if (resourceScope == RoleScope.PROGRAM)
            {
                return ScopeId == resourceId;
            }

            return resourceScope == RoleScope.ACTIVITY && ScopeId == resourceProgramId;
        }

        return false;
    }
}
