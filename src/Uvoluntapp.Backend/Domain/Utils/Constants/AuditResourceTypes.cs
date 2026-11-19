using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Activities;

namespace Uvoluntapp.Backend.Domain.Utils.Constants;

public static class AuditResourceTypes
{
    public const string User = "USER";

    public const string RoleAssignment = "ROLE_ASSIGNMENT";

    public const string AccountApprovalRequest = "ACCOUNT_APPROVAL_REQUEST";

    public const string Program = "PROGRAM";

    public const string Activity = "ACTIVITY";
}
