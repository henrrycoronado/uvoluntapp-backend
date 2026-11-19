using Uvoluntapp.Backend.Domain.Entities.Common;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed class AdminGrantApproval : Entity
{
    public Guid RequestId { get; set; }

    public Guid AdminId { get; set; }

    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
}
