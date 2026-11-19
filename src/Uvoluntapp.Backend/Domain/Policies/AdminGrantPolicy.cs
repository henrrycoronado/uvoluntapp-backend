using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Domain.Policies;

/// <summary>
/// Reglas de la promoción a ADMIN (SCRUM-107): dos aprobaciones distintas,
/// ninguna del beneficiario, ambas de ADMIN activos, y nunca menos de dos ADMIN activos.
/// </summary>
public static class AdminGrantPolicy
{
    public const int MinimumActiveAdmins = 2;

    public static void EnsureCanApprove(
        Guid beneficiaryId,
        Guid firstApproverId,
        Guid secondApproverId,
        IReadOnlySet<Guid> activeAdminIds)
    {
        if (firstApproverId == secondApproverId)
        {
            throw new DomainRuleViolationException("Los dos aprobadores deben ser distintos.");
        }

        if (firstApproverId == beneficiaryId || secondApproverId == beneficiaryId)
        {
            throw new DomainRuleViolationException("El beneficiario no puede aprobar su propia promoción.");
        }

        if (!activeAdminIds.Contains(firstApproverId) || !activeAdminIds.Contains(secondApproverId))
        {
            throw new DomainRuleViolationException("Ambos aprobadores deben ser ADMIN activos.");
        }
    }

    public static void EnsureCanRemoveAdmin(int activeAdminCount)
    {
        if (activeAdminCount - 1 < MinimumActiveAdmins)
        {
            throw new DomainRuleViolationException("Debe quedar al menos dos ADMIN activos.");
        }
    }
}
