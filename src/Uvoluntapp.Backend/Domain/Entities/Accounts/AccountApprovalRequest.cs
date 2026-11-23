using Uvoluntapp.Backend.Domain.Entities.Common;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

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

    public void Approve(Guid reviewerId, DateTime reviewedAt)
    {
        EnsurePending();
        Status = AccountApprovalStatus.ACTIVE;
        ReviewedBy = reviewerId;
        ReviewedAt = reviewedAt;
        RejectionReason = null;
    }

    public void Reject(Guid reviewerId, string reason, DateTime reviewedAt)
    {
        EnsurePending();
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("El motivo del rechazo es obligatorio.", nameof(reason));
        }

        Status = AccountApprovalStatus.REJECTED;
        ReviewedBy = reviewerId;
        ReviewedAt = reviewedAt;
        RejectionReason = reason.Trim();
    }

    public bool CanRequestAgainAt(DateTime instant)
    {
        return Status == AccountApprovalStatus.REJECTED
            && ReviewedAt is not null
            && instant >= ReviewedAt.Value.AddHours(12);
    }

    public AccountApprovalRequest CreateResubmission()
    {
        if (Status != AccountApprovalStatus.REJECTED)
        {
            throw new InvalidOperationException("Solo una solicitud rechazada puede volver a solicitarse.");
        }

        return new AccountApprovalRequest
        {
            UserId = UserId,
            FirstName = FirstName,
            LastName = LastName,
            Phone = Phone,
            Relationship = Relationship,
        };
    }

    private void EnsurePending()
    {
        if (Status != AccountApprovalStatus.PENDING_APPROVAL)
        {
            throw new InvalidOperationException("La solicitud ya fue resuelta.");
        }
    }
}
