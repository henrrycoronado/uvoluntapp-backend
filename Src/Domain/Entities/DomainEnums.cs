namespace U_VoluntApp_Core.Src.Domain.Entities;

// Los nombres son los valores persistidos y publicados en OpenAPI.
public enum AccountStatus
{
    PRE_REGISTERED,
    PENDING_VERIFICATION,
    PENDING_APPROVAL,
    ACTIVE,
    REJECTED,
    SUSPENDED,
}

public enum AccountApprovalStatus
{
    PENDING_APPROVAL,
    ACTIVE,
    REJECTED,
}

public enum AdminGrantStatus
{
    PENDING,
    APPROVED,
    REJECTED,
}

public enum AssignedRole
{
    ADMIN = 1,
    PROGRAM_LEADER,
    OPERATIONAL_LEADER,
}

public enum RoleScope
{
    GLOBAL,
    PROGRAM,
    ACTIVITY,
}

public enum ProgramStatus
{
    DRAFT,
    ACTIVE,
    INACTIVE,
}

public enum ActivityStatus
{
    DRAFT,
    PUBLISHED,
    IN_PROGRESS,
    FINISHED,
    CANCELLED,
}

public enum ActivityModality
{
    IN_PERSON = 1,
    VIRTUAL,
    HYBRID,
}
