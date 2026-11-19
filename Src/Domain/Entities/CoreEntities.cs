namespace U_VoluntApp_Core.Src.Domain.Entities;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime? DeletedAt { get; protected set; }
}

public sealed class Profile : Entity
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Guid? CareerId { get; set; }

    public string? Phone { get; set; }
}

public sealed class Career : Entity
{
    public string Name { get; set; } = string.Empty;
}

public sealed class AuthSession : Entity
{
    public Guid UserId { get; set; }

    public string RefreshTokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}

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

public sealed class RoleAssignment : Entity
{
    public Guid UserId { get; set; }

    public required AssignedRole Role { get; set; }

    public RoleScope Scope { get; set; } = RoleScope.GLOBAL;

    public Guid? ScopeId { get; set; }

    public Guid GrantedBy { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime? RevokedAt { get; set; }
}

public sealed class AdminGrantRequest : Entity
{
    public Guid BeneficiaryId { get; set; }

    public Guid RequestedBy { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public AdminGrantStatus Status { get; set; } = AdminGrantStatus.PENDING;
}

public sealed class AdminGrantApproval : Entity
{
    public Guid RequestId { get; set; }

    public Guid AdminId { get; set; }

    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
}

public sealed class AcademicPeriod : Entity
{
    public string Label { get; set; } = string.Empty;

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }
}

public sealed class Program : Entity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public ProgramStatus Status { get; private set; } = ProgramStatus.DRAFT;

    public void Activate()
    {
        if (Status is not (ProgramStatus.DRAFT or ProgramStatus.INACTIVE))
        {
            throw new InvalidOperationException();
        }

        Status = ProgramStatus.ACTIVE;
    }

    public void Deactivate()
    {
        if (Status != ProgramStatus.ACTIVE)
        {
            throw new InvalidOperationException();
        }

        Status = ProgramStatus.INACTIVE;
    }
}

public sealed class ProgramPeriod : Entity
{
    public Guid ProgramId { get; set; }

    public Guid AcademicPeriodId { get; set; }
}

public sealed class Activity : Entity
{
    public Guid? ProgramId { get; set; }

    public Guid AcademicPeriodId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public required ActivityModality Modality { get; set; }

    public string? Location { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public int? Capacity { get; set; }

    public string? PeriodicityNote { get; set; }

    public string? CancellationReason { get; private set; }

    public ActivityStatus Status { get; private set; } = ActivityStatus.DRAFT;

    public DateTime? StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    public void Publish()
    {
        if (Status != ActivityStatus.DRAFT)
        {
            throw new InvalidOperationException();
        }

        Status = ActivityStatus.PUBLISHED;
    }

    public void Cancel(string reason)
    {
        if (Status is not (ActivityStatus.DRAFT or ActivityStatus.PUBLISHED) || string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException();
        }

        CancellationReason = reason;
        Status = ActivityStatus.CANCELLED;
    }

    public void Finish()
    {
        if (Status != ActivityStatus.IN_PROGRESS)
        {
            throw new InvalidOperationException();
        }

        Status = ActivityStatus.FINISHED;
        FinishedAt = DateTime.UtcNow;
    }
}

public sealed class ActivityReschedule : Entity
{
    public Guid ActivityId { get; set; }

    public Guid AuthorId { get; set; }

    public DateTime PreviousStartsAt { get; set; }

    public DateTime PreviousEndsAt { get; set; }

    public DateTime NewStartsAt { get; set; }

    public DateTime NewEndsAt { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public sealed class AuditEvent : Entity
{
    public Guid? ActorId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid ResourceId { get; set; }

    public string Data { get; set; } = "{}";
}
