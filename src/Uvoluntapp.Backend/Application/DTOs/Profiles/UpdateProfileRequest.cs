namespace Uvoluntapp.Backend.Application.DTOs.Profiles;

public sealed record UpdateProfileRequest(
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Email = null,
    IReadOnlySet<ProfileUpdateField>? Fields = null);
