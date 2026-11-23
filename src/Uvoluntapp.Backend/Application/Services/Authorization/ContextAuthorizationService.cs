using Uvoluntapp.Backend.Application.DTOs.Authorization;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Authorization;

public sealed class ContextAuthorizationService
{
    public AuthorizationDecision Authorize(AuthorizationRequest request)
    {
        if (!request.IsAuthenticated)
        {
            return AuthorizationDecision.AUTHENTICATION_REQUIRED;
        }

        if (request.AccountStatus != AccountStatus.ACTIVE)
        {
            return AuthorizationDecision.ACCOUNT_NOT_ACTIVE;
        }

        var now = request.AtUtc ?? DateTime.UtcNow;
        var assignments = request.Assignments.Where(candidate =>
            candidate.Role == request.RequiredRole &&
            candidate.IsActiveAt(now)).ToList();

        if (assignments.Count == 0)
        {
            return AuthorizationDecision.ASSIGNMENT_NOT_ACTIVE;
        }

        if (!assignments.Any(a => a.Covers(request.ResourceScope, request.ResourceId, request.ResourceProgramId)))
        {
            return AuthorizationDecision.SCOPE_NOT_COVERED;
        }

        if (!request.ResourceStateAllowsOperation)
        {
            return AuthorizationDecision.RESOURCE_STATE_NOT_ALLOWED;
        }

        return AuthorizationDecision.ALLOWED;
    }
}
