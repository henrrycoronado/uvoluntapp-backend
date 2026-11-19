using System.Diagnostics.CodeAnalysis;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Domain.Entities.Accounts;

// Correo normalizado para comparar y almacenar de forma consistente.
public sealed class EmailAddress
{
    private const int MaxLength = 256;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public bool IsInstitutional => InstitutionalEmailDomain.IsInstitutional(Value);

    public static EmailAddress Create(string? value)
    {
        if (!TryCreate(value, out var address))
        {
            throw new ArgumentException("El correo no tiene un formato válido.", nameof(value));
        }

        return address;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out EmailAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength || normalized.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var at = normalized.IndexOf('@');
        if (at <= 0 || at != normalized.LastIndexOf('@') || at == normalized.Length - 1)
        {
            return false;
        }

        address = new EmailAddress(normalized);
        return true;
    }
}
