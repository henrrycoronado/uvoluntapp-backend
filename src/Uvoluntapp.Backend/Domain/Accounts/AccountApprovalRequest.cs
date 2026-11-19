namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed class AccountApprovalRequest : Entity
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string Relationship { get; set; } = string.Empty;

    public AccountApprovalStatus Status { get; set; } = AccountApprovalStatus.PENDING_APPROVAL;

    public string? RejectionReason { get; set; }
}
