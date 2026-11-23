using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Domain.Utils.Exceptions;

namespace Uvoluntapp.Backend.Domain.Policies;

public static class GoogleAccountLinkingPolicy
{
    public static GoogleAccountLinkDecision Decide(
        GoogleIdentity identity,
        bool accountExists,
        bool accountEmailVerified,
        DateTime? now = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (string.IsNullOrWhiteSpace(identity.Subject)
            || string.IsNullOrWhiteSpace(identity.Email)
            || !identity.EmailVerified)
        {
            throw new DomainRuleViolationException("La identidad de Google no está verificada.");
        }

        var institutional = Uvoluntapp.Backend.Domain.Utils.Constants.InstitutionalEmailDomain.IsInstitutional(identity.Email) && string.Equals(identity.HostedDomain, "ucb.edu.bo", StringComparison.OrdinalIgnoreCase);
        if (accountExists && (!accountEmailVerified || !identity.EmailVerified))
        {
            throw new DomainRuleViolationException("No se puede vincular la identidad externa.");
        }

        return new GoogleAccountLinkDecision(
            institutional ? AccountStatus.ACTIVE : AccountStatus.PENDING_APPROVAL,
            !accountExists,
            accountExists,
            institutional);
    }
}
