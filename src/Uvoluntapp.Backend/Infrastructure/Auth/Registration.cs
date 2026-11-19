using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Services.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Audit;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;
using Uvoluntapp.Backend.Infrastructure.Identity;
using Uvoluntapp.Backend.Infrastructure.Persistence;

namespace Uvoluntapp.Backend.Infrastructure.Auth;

public sealed class Registration(
    AppDbContext db,
    UserManager<AppUser> users,
    AccountRateLimiter limits,
    RegistrationCodes codes,
    IEmailSender mail,
    IWebLinks links,
    SessionTokens tokens,
    TimeProvider clock,
    ILogger<Registration> logger) : IRegistration
{
    public async Task RegisterAsync(RegisterRequest request, string ip, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var normalized = users.NormalizeEmail(email)!;
        await limits.CheckAsync("register", normalized, ip);

        // Validate public input before looking up the address, including Identity's email column limit.
        if (email.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            throw new AccountException(AccountError.InvalidInput);
        }

        await using var tx = await LockAsync(normalized, cancellationToken);
        var user = await FindUserAsync(normalized, cancellationToken);
        string? stamp = null;

        // Una cuenta creada con Google no tiene contraseña: recibe el código para crearla, con la misma respuesta pública.
        if (user is { PasswordHash: null, EmailConfirmed: true, DeletedAt: null, Status: AccountStatus.ACTIVE or AccountStatus.PENDING_APPROVAL })
        {
            var setupCode = await codes.IssueAsync(normalized, user.SecurityStamp);
            await tx.CommitAsync(cancellationToken);
            await DeliverPasswordSetupAsync(user.Email!, setupCode!, cancellationToken);
            return;
        }

        // Una cuenta pre-registrada (importación histórica) se reclama: conserva su perfil e historial.
        var claim = user is { Status: AccountStatus.PRE_REGISTERED, DeletedAt: null };
        if (user is null || claim || (user.Status == AccountStatus.PENDING_VERIFICATION && !user.EmailConfirmed && user.DeletedAt is null))
        {
            if (user is null)
            {
                user = new AppUser { Id = Guid.NewGuid(), UserName = email, Email = email, Status = AccountStatus.PENDING_VERIFICATION };
                var result = await users.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    throw new AccountException(AccountError.InvalidInput);
                }
            }
            else
            {
                // Bind the new code to the credentials of this registration; old codes stop working.
                user.PasswordHash = users.PasswordHasher.HashPassword(user, request.Password);
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.Status = AccountStatus.PENDING_VERIFICATION;
            }

            var profile = await ProfileAsync(user.Id, cancellationToken);
            if (!claim || string.IsNullOrWhiteSpace(profile.FirstName))
            {
                profile.FirstName = request.FirstName;
                profile.LastName = request.LastName;
            }

            if (!string.IsNullOrWhiteSpace(request.Phone) && (!claim || string.IsNullOrWhiteSpace(profile.Phone)))
            {
                profile.Phone = request.Phone.Trim();
            }

            await db.SaveChangesAsync(cancellationToken);
            stamp = user.SecurityStamp;
        }

        var code = await codes.IssueAsync(normalized, stamp);
        await tx.CommitAsync(cancellationToken);
        if (code is not null)
        {
            await DeliverAsync(email, code, cancellationToken);
        }
    }

    public async Task ResendAsync(string email, string ip, CancellationToken cancellationToken)
    {
        var normalized = users.NormalizeEmail(email.Trim())!;
        await limits.CheckAsync("register", normalized, ip);
        await using var tx = await LockAsync(normalized, cancellationToken);
        var user = await FindUserAsync(normalized, cancellationToken);
        var eligible = user is { DeletedAt: null } && (user.Status == AccountStatus.PENDING_VERIFICATION || await NeedsApprovalDataAsync(user, cancellationToken));
        var code = await codes.IssueAsync(normalized, eligible ? user!.SecurityStamp : null);
        await tx.CommitAsync(cancellationToken);
        if (code is not null)
        {
            await DeliverAsync(user!.Email!, code, cancellationToken);
        }
    }

    public async Task<VerifyOtpResponse> VerifyAsync(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var normalized = users.NormalizeEmail(request.Email.Trim())!;
        await using var tx = await LockAsync(normalized, cancellationToken);
        var user = await FindUserAsync(normalized, cancellationToken);
        await codes.VerifyAsync(normalized, user?.SecurityStamp, request.Code);
        if (user is null || user.DeletedAt.HasValue ||
            (user.Status != AccountStatus.PENDING_VERIFICATION && !await NeedsApprovalDataAsync(user, cancellationToken)))
        {
            throw new AccountException(AccountError.InvalidCode);
        }

        user.EmailConfirmed = true;
        user.SecurityStamp = Guid.NewGuid().ToString();
        var institutional = InstitutionalEmailPolicy.IsInstitutional(user.Email);
        user.Status = institutional ? AccountStatus.ACTIVE : AccountStatus.PENDING_APPROVAL;
        TokenPair? pair = null;
        if (institutional)
        {
            user.InstitutionalEmail = user.Email;
            user.InstitutionalEmailVerifiedAt = clock.GetUtcNow().UtcDateTime;
            var issued = tokens.Issue(db.AuthSessions.Add(new AuthSession { UserId = user.Id }).Entity);
            pair = new TokenPair { AccessToken = issued.AccessToken, RefreshToken = issued.RefreshToken, ExpiresAt = issued.ExpiresAt };
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new VerifyOtpResponse { AccountStatus = user.Status.ToString(), RequiresApprovalData = !institutional, Tokens = pair };
    }

    public async Task CompleteAsync(CompleteRegistrationRequest request, CancellationToken cancellationToken)
    {
        var normalized = users.NormalizeEmail(request.Email.Trim())!;
        await using var tx = await LockAsync(normalized, cancellationToken);
        var user = await FindUserAsync(normalized, cancellationToken);
        if (user is null || user.DeletedAt.HasValue || !await NeedsApprovalDataAsync(user, cancellationToken))
        {
            throw new AccountException(AccountError.RegistrationConflict);
        }

        var profile = await ProfileAsync(user.Id, cancellationToken);
        profile.FirstName = request.FirstName;
        profile.LastName = request.LastName;
        profile.Phone = request.Phone;
        db.AccountApprovalRequests.Add(new AccountApprovalRequest
        {
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Relationship = request.RelationWithPastoral
        });
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task ReapplyAsync(ReapplyRequest request, string ip, CancellationToken cancellationToken)
    {
        var normalized = users.NormalizeEmail(request.Email.Trim())!;
        await limits.CheckAsync("login", normalized, ip);
        await using var tx = await LockAsync(normalized, cancellationToken);
        var user = await FindUserAsync(normalized, cancellationToken);
        if (user is null || user.DeletedAt.HasValue || !await users.CheckPasswordAsync(user, request.Password))
        {
            if (user is not null)
            {
                await users.AccessFailedAsync(user);
                await tx.CommitAsync(cancellationToken);
            }

            throw new AccountException(AccountError.InvalidSession);
        }

        var last = await db.AccountApprovalRequests
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (user.Status != AccountStatus.REJECTED || last is null)
        {
            throw new AccountException(AccountError.RegistrationConflict);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        if (!last.CanRequestAgainAt(now))
        {
            throw new AccountException(
                AccountError.ReapplyTooEarly,
                new Dictionary<string, object?> { ["reapplyAvailableAt"] = last.ReviewedAt?.AddDays(1) });
        }

        // La solicitud anterior queda como historial; la nueva lleva los datos actualizados.
        var next = last.CreateResubmission();
        next.FirstName = request.FirstName.Trim();
        next.LastName = request.LastName.Trim();
        next.Phone = request.Phone.Trim();
        next.Relationship = request.RelationWithPastoral.Trim();
        db.AccountApprovalRequests.Add(next);
        var profile = await ProfileAsync(user.Id, cancellationToken);
        profile.FirstName = next.FirstName;
        profile.LastName = next.LastName;
        profile.Phone = next.Phone;
        user.Status = AccountStatus.PENDING_APPROVAL;
        db.AuditEvents.Add(AuditEvent.Create(user.Id, AuditActions.AccountReapplied, AuditResourceTypes.AccountApprovalRequest, next.Id, null, now));
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task<Profile> ProfileAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Profiles.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken)
        ?? db.Profiles.Add(new Profile { UserId = userId }).Entity;

    private Task<AppUser?> FindUserAsync(string normalized, CancellationToken cancellationToken) =>
        db.Users.FromSqlInterpolated($"SELECT * FROM users WHERE normalized_email = {normalized} FOR UPDATE").SingleOrDefaultAsync(cancellationToken);

    private async Task<IDbContextTransaction> LockAsync(string normalized, CancellationToken cancellationToken)
    {
        var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        // A row lock alone cannot serialize two first registrations of the same email.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({normalized}, 0))", cancellationToken);
        return tx;
    }

    private async Task<bool> NeedsApprovalDataAsync(AppUser user, CancellationToken cancellationToken) =>
        user.EmailConfirmed && user.Status == AccountStatus.PENDING_APPROVAL
        && !await db.AccountApprovalRequests.AnyAsync(r => r.UserId == user.Id, cancellationToken);

    private async Task DeliverPasswordSetupAsync(string email, string code, CancellationToken cancellationToken)
    {
        try
        {
            await mail.SendAsync(
                email,
                "Crea tu contraseña en Uvoluntapp",
                $"Tu cuenta ya existe y entraste antes con Google. Para crear tu contraseña usa el código {code} (vence en 10 minutos) en {links.CreatePassword(email)}",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError("El proveedor de correo no pudo entregar el código de contraseña ({ErrorType}).", ex.GetType().Name);
        }
    }

    private async Task DeliverAsync(string email, string code, CancellationToken cancellationToken)
    {
        try
        {
            await mail.SendAsync(email, "Verifica tu registro en Uvoluntapp", $"Tu código de verificación es {code}.", cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // Keep the public response uniform. A resend issues a new challenge after provider failure.
            logger.LogError("El proveedor de correo no pudo entregar el OTP de registro ({ErrorType}).", ex.GetType().Name);
        }
    }
}
