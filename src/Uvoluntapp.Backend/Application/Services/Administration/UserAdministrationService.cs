using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Services.Accounts;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Constants;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Administration;

/// <summary>Consulta de usuarios y bajas o reactivaciones de cuentas (SCRUM-61).</summary>
public sealed class UserAdministrationService(
    AdminAccessGuard guard,
    IAccountDirectory accounts,
    IRoleAssignmentStore roles,
    AccountSuspensionService suspension,
    AuditRecorder audit,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<PaginatedItems<UserSummaryResponse>> ListAsync(
        Guid actorId,
        AccountStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await RequireReviewerAsync(actorId, cancellationToken);
        var result = await accounts.SearchAsync(status, search, page, pageSize, cancellationToken);
        return new PaginatedItems<UserSummaryResponse>(
            result.Items.Select(u => new UserSummaryResponse(
                u.Id, u.Email, u.FirstName, u.LastName, u.Status.ToString(), u.CreatedAt, u.VerifiedAt)).ToList(),
            new(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<UserDetailResponse> GetAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await RequireReviewerAsync(actorId, cancellationToken);
        var user = await accounts.FindAsync(userId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe.");
        var assignments = await roles.ListForUserAsync(userId, cancellationToken);

        return new UserDetailResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.InstitutionalEmail,
            user.InstitutionalEmailVerifiedAt,
            user.Status.ToString(),
            assignments.Select(RoleAssignmentResponse.From).ToList(),
            user.CreatedAt,
            user.SuspendedAt,
            user.VerifiedAt,
            user.VerifiedBy);
    }

    // Verificación manual: un ADMIN responde por la identidad de una cuenta sin correo institucional.
    public async Task VerifyAsync(Guid actorId, Guid userId, CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var user = await accounts.FindAsync(userId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe.");
        if (user.VerifiedAt is not null)
        {
            throw new ConflictException("La cuenta ya está verificada.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        await accounts.SetVerificationAsync(userId, now, actorId, cancellationToken);
        await audit.RecordAsync(actorId, AuditActions.AccountVerified, AuditResourceTypes.User, userId, null, now, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Para quien perdió el acceso a su correo (egresados). Sin OTP: lo decide un ADMIN con motivo.
    // La cuenta queda verificada solo si el correo nuevo es institucional.
    public async Task ChangeEmailAsync(
        Guid actorId,
        Guid userId,
        ChangeUserEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("El motivo es obligatorio.", nameof(request));
        }

        var user = await accounts.FindAsync(userId, cancellationToken)
            ?? throw new NotFoundException("El usuario no existe.");
        var now = clock.GetUtcNow().UtcDateTime;
        if (!await accounts.ReplaceAccessEmailAsync(userId, request.NewEmail, now, cancellationToken))
        {
            throw new ConflictException("Ese correo no se puede usar.");
        }

        var institutional = InstitutionalEmailDomain.IsInstitutional(request.NewEmail.Trim());
        await accounts.SetVerificationAsync(userId, institutional ? now : null, null, cancellationToken);
        await audit.RecordAsync(
            actorId,
            AuditActions.AccessEmailReplacedByAdmin,
            AuditResourceTypes.User,
            userId,
            new Dictionary<string, object?> { ["reason"] = request.Reason.Trim(), ["verified"] = institutional },
            now,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SuspendAsync(
        Guid actorId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var actorRoles = await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var result = await suspension.SuspendAsync(userId, reason, actorId, actorRoles, null, cancellationToken);

        await accounts.SetStatusAsync(userId, result.Status, result.SuspendedAt, cancellationToken);
        await accounts.RevokeSessionsAsync(result.SessionIdsToRevoke, result.SuspendedAt, cancellationToken);
        await audit.RecordAccountStatusChangedAsync(
            actorId, userId, AccountStatus.ACTIVE, result.Status, result.Reason, result.SuspendedAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ReactivateAsync(
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var actorRoles = await guard.RequireGlobalAdminAsync(actorId, cancellationToken);
        var result = await suspension.ReactivateAsync(userId, actorId, actorRoles, null, cancellationToken);

        await accounts.SetStatusAsync(userId, result.Status, null, cancellationToken);
        await audit.RecordAccountStatusChangedAsync(
            actorId, userId, AccountStatus.SUSPENDED, result.Status, null, result.ReactivatedAt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RequireReviewerAsync(Guid actorId, CancellationToken cancellationToken)
    {
        var actor = await guard.ResolveAsync(actorId, cancellationToken);
        if (!actor.CanReview)
        {
            throw new ForbiddenException("Solo un ADMIN o un líder puede consultar usuarios.");
        }
    }
}
