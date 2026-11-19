using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Google;

public sealed class GoogleAuthService(
    IGoogleTokenValidator tokenValidator,
    IGoogleAccountStore accounts,
    ISessionIssuer? sessions = null)
{
    public async Task<GoogleAuthResponse> AuthenticateAsync(
        GoogleAuthRequest request,
        IReadOnlyCollection<string> allowedAudiences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(allowedAudiences);
        if (string.IsNullOrWhiteSpace(request.IdToken) || allowedAudiences.Count == 0)
        {
            throw new AccountException(AccountError.InvalidInput);
        }

        var claims = await tokenValidator.ValidateAsync(
            request.IdToken,
            allowedAudiences,
            cancellationToken);
        if (claims is null
            || !allowedAudiences.Contains(claims.Audience, StringComparer.Ordinal)
            || !claims.EmailVerified)
        {
            throw new AccountException(AccountError.InvalidInput);
        }

        var identity = new GoogleIdentity(
            claims.Subject,
            claims.Email.Trim(),
            claims.EmailVerified,
            claims.HostedDomain,
            claims.GivenName?.Trim(),
            claims.FamilyName?.Trim());
        var bySubject = await accounts.FindBySubjectAsync(identity.Subject, cancellationToken);
        var account = bySubject ?? await accounts.FindByEmailAsync(identity.Email, cancellationToken);
        var decision = GoogleAccountLinkingPolicy.Decide(
            identity,
            account is not null,
            account?.EmailVerified == true);

        if (account is not null && decision.LinkExisting && bySubject is null)
        {
            await accounts.LinkAsync(account.UserId, identity, cancellationToken);
        }

        if (account is null)
        {
            account = await accounts.CreateAsync(
                identity,
                decision.Status,
                decision.InstitutionalEmailVerified,
                cancellationToken);
        }

        // Solo una cuenta activa abre sesión; una externa nueva espera la aprobación de un ADMIN.
        var tokens = account.Status == AccountStatus.ACTIVE && sessions is not null
            ? await sessions.IssueAsync(account.UserId, cancellationToken)
            : null;

        // Una cuenta externa verificada completa sus datos antes de llegar a la cola de aprobación.
        var requiresApprovalData = account.Status == AccountStatus.PENDING_APPROVAL
            && account.EmailVerified
            && !await accounts.HasApprovalRequestAsync(account.UserId, cancellationToken);
        return new GoogleAuthResponse(
            account.Status.ToString(),
            decision.IsNewAccount,
            tokens,
            account.Email,
            requiresApprovalData,
            requiresApprovalData ? identity.GivenName : null,
            requiresApprovalData ? identity.FamilyName : null);
    }
}
