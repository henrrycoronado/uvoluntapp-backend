namespace Uvoluntapp.Backend.Application.Accounts.Recovery;

public interface IRecoveryTokenGenerator
{
    (string RawToken, string TokenHash) Create();

    string Hash(string rawToken);
}
