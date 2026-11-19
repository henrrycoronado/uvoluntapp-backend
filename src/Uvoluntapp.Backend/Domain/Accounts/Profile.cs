namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed class Profile : Entity
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Guid? CareerId { get; set; }

    public string? Phone { get; set; }
}
