namespace Uvoluntapp.Backend.Domain.Accounts;

// SCRUM-109: un correo solo es institucional si su dominio es exactamente ucb.edu.bo.
public static class InstitutionalEmailDomain
{
    public const string Domain = "ucb.edu.bo";

    public const string Suffix = "@" + Domain;

    public static bool IsInstitutional(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var trimmed = email.Trim();
        return trimmed.Length > Suffix.Length
            && trimmed.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase);
    }
}
