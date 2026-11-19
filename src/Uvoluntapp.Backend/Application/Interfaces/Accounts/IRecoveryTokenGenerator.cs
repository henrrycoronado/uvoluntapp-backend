namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IRecoveryTokenGenerator
{
    (string RawToken, string TokenHash) Create();

    string Hash(string rawToken);
}
