using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Administration.Ports;

public interface IAdminGrantStore
{
    Task<AdminGrantRequest?> FindRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminGrantRequest>> SearchRequestsAsync(
        AdminGrantStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingRequestAsync(Guid beneficiaryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminGrantApproval>> ListApprovalsAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task AddRequestAsync(AdminGrantRequest request, CancellationToken cancellationToken = default);

    Task AddApprovalAsync(AdminGrantApproval approval, CancellationToken cancellationToken = default);
}
