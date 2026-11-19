using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Administration;

public interface IAccountDirectory
{
    Task<AccountStatus?> FindStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserRecord?> FindAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<UserRecord>> SearchAsync(
        AccountStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SetStatusAsync(
        Guid userId,
        AccountStatus status,
        DateTime? suspendedAt,
        CancellationToken cancellationToken = default);

    Task SetVerificationAsync(
        Guid userId,
        DateTime? verifiedAt,
        Guid? verifiedBy,
        CancellationToken cancellationToken = default);

    // Cambia el correo de acceso sin OTP (lo hace un ADMIN), rota el sello y revoca todas las sesiones.
    // Devuelve false si el correo ya pertenece a otra cuenta.
    Task<bool> ReplaceAccessEmailAsync(
        Guid userId,
        string newEmail,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task RevokeSessionsAsync(
        IReadOnlyList<Guid> sessionIds,
        DateTime revokedAt,
        CancellationToken cancellationToken = default);
}
