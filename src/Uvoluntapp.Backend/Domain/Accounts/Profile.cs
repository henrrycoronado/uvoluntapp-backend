namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed class Profile : Entity
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Guid? CareerId { get; set; }

    public string? Phone { get; set; }

    public void Update(string? firstName, string? lastName, string? phone)
    {
        if (firstName is not null)
        {
            FirstName = RequireValue(firstName, nameof(firstName));
        }

        if (lastName is not null)
        {
            LastName = RequireValue(lastName, nameof(lastName));
        }

        Phone = phone;
    }

    private static string RequireValue(string value, string parameterName)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0
            ? throw new ArgumentException("El valor no puede estar vacío.", parameterName)
            : trimmed;
    }
}
