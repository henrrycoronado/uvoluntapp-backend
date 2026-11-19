using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Administration;

public interface IAccountApprovalStore
{
    Task<AccountApprovalRequest?> FindAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<PagedResult<ApprovalRequestView>> SearchAsync(
        AccountApprovalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
