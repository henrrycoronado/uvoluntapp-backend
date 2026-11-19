using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IGoogleAccountStore
{
    Task<GoogleAccountRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<GoogleAccountRecord?> FindBySubjectAsync(string subject, CancellationToken cancellationToken);

    Task<GoogleAccountRecord> CreateAsync(
        GoogleIdentity identity,
        AccountStatus status,
        bool institutionalEmailVerified,
        CancellationToken cancellationToken);

    // Indica si la cuenta ya envió su solicitud de aprobación.
    Task<bool> HasApprovalRequestAsync(Guid userId, CancellationToken cancellationToken);

    Task LinkAsync(
        Guid userId,
        GoogleIdentity identity,
        CancellationToken cancellationToken);
}
