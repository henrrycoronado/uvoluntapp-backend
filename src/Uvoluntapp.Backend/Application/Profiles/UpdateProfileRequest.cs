namespace Uvoluntapp.Backend.Application.Profiles;

public sealed record UpdateProfileRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Email = null,
    IReadOnlySet<ProfileUpdateField>? Fields = null);
