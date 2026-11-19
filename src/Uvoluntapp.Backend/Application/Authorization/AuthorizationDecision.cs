namespace Uvoluntapp.Backend.Application.Authorization;

public enum AuthorizationDecision
{
    ALLOWED,
    AUTHENTICATION_REQUIRED,
    ACCOUNT_NOT_ACTIVE,
    ASSIGNMENT_NOT_ACTIVE,
    SCOPE_NOT_COVERED,
    RESOURCE_STATE_NOT_ALLOWED,
}
