using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Ports;

public interface IAccountApprovalStore
{
    Task<AccountApprovalRequest?> FindAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<PagedResult<ApprovalRequestView>> SearchAsync(
        AccountApprovalStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
