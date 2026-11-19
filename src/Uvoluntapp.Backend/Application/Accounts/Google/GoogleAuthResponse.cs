using Uvoluntapp.Backend.Application.Accounts.Contracts;

namespace Uvoluntapp.Backend.Application.Accounts.Google;

public sealed record GoogleAuthResponse(
    string AccountStatus,
    bool IsNewAccount,
    TokenPair? Tokens);
