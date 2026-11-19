using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.DTOs.Administration;

public sealed record ApprovalRequestView(AccountApprovalRequest Request, string Email);
