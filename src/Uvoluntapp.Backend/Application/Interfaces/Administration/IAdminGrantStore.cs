using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Interfaces.Administration;

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
