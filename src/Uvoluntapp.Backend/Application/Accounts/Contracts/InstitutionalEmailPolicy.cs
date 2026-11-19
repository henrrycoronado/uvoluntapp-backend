namespace Uvoluntapp.Backend.Application.Accounts.Contracts;

public static class InstitutionalEmailPolicy
{
    public const string Domain = "ucb.edu.bo";

    public static bool IsInstitutional(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return email.EndsWith("@" + Domain, StringComparison.OrdinalIgnoreCase);
    }
}
