using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

public sealed class AdminGrantRequest : Entity
{
    public Guid BeneficiaryId { get; set; }

    public Guid RequestedBy { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public AdminGrantStatus Status { get; set; } = AdminGrantStatus.PENDING;
}
