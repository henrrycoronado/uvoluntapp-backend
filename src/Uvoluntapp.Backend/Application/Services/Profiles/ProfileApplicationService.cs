using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Profiles;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Administration;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Application.Interfaces.Profiles;
using Uvoluntapp.Backend.Application.Services.Accounts;
using Uvoluntapp.Backend.Application.Services.Audit;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Application.Services.Profiles;

/// <summary>Perfil propio y correos de la cuenta (SCRUM-59 y SCRUM-109).</summary>
public sealed class ProfileApplicationService(
    IProfileStore profiles,
    IAccountDirectory accounts,
    IRoleAssignmentStore roles,
    AccountEmailService emails,
    ProfileService profileRules,
    IEmailSender mail,
    IOneTimeCodeGenerator codes,
    IUnitOfWork unitOfWork,
    AuditRecorder audit,
    TimeProvider clock)
{
    private static readonly HashSet<ProfileUpdateField> _editableFields =
        [ProfileUpdateField.FIRST_NAME, ProfileUpdateField.LAST_NAME, ProfileUpdateField.PHONE];

    public async Task<ProfileDetailResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.FindByUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException("El perfil no existe.");
        var user = await accounts.FindAsync(userId, cancellationToken)
            ?? throw new NotFoundException("La cuenta no existe.");
        var assignments = await roles.ListForUserAsync(userId, cancellationToken);
        return new ProfileDetailResponse(
            userId,
            user.Email,
            profile.FirstName,
            profile.LastName,
            profile.Phone,
            user.InstitutionalEmail,
            user.InstitutionalEmailVerifiedAt,
            user.Status.ToString(),
            assignments.Select(RoleAssignmentResponse.From).ToList(),
            user.CreatedAt);
    }

    public async Task<ProfileDetailResponse> UpdateAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await profiles.FindByUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException("El perfil no existe.");
        profileRules.UpdateMyProfile(userId, profile, request, _editableFields);
        var fields = new[] { ("firstName", request.FirstName), ("lastName", request.LastName), ("phone", request.Phone) }
            .Where(f => f.Item2 is not null)
            .Select(f => f.Item1)
            .ToList();
        await audit.RecordAsync(
            userId,
            AuditActions.ProfileUpdated,
            AuditResourceTypes.User,
            userId,
            new Dictionary<string, object?> { ["fields"] = fields },
            clock.GetUtcNow().UtcDateTime,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, cancellationToken);
    }

    public async Task<string> RequestEmailChangeAsync(
        Guid userId,
        ChangeEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = codes.Create();
        var result = await emails.RequestEmailChangeAsync(
            userId, request.NewEmail, request.Password, code, clock.GetUtcNow(), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await mail.SendAsync(result.Email, "Confirma tu nuevo correo", $"Tu código es {code}. Vence en 10 minutos.", cancellationToken);
        return "Enviamos un código al nuevo correo.";
    }

    public async Task<string> VerifyEmailChangeAsync(
        Guid userId,
        VerifyCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        var result = await emails.ConfirmEmailChangeAsync(userId, request.Code, now, cancellationToken);
        await accounts.RevokeSessionsAsync(result.SessionIdsToRevoke, now.UtcDateTime, cancellationToken);
        await audit.RecordAsync(
            userId,
            AuditActions.AccessEmailChanged,
            AuditResourceTypes.User,
            userId,
            new Dictionary<string, object?> { ["revokedSessions"] = result.SessionIdsToRevoke.Count },
            now.UtcDateTime,
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return "Correo actualizado. Vuelve a iniciar sesión.";
    }

    public async Task<string> SetInstitutionalEmailAsync(
        Guid userId,
        SetInstitutionalEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = codes.Create();
        var result = await emails.RequestInstitutionalEmailAsync(
            userId, request.InstitutionalEmail, code, clock.GetUtcNow(), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await mail.SendAsync(result.Email, "Verifica tu correo institucional", $"Tu código es {code}. Vence en 10 minutos.", cancellationToken);
        return "Enviamos un código al correo institucional.";
    }

    public async Task<string> VerifyInstitutionalEmailAsync(
        Guid userId,
        VerifyCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        await emails.ConfirmInstitutionalEmailAsync(userId, request.Code, now, cancellationToken);
        await audit.RecordAsync(userId, AuditActions.InstitutionalEmailVerified, AuditResourceTypes.User, userId, null, now.UtcDateTime, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return "Correo institucional verificado.";
    }
}
