using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Profiles;

public sealed class ProfileService
{
    public ProfileResponse GetMyProfile(Guid actorId, Profile profile)
    {
        EnsureOwner(actorId, profile);
        return ToResponse(profile);
    }

    public ProfileResponse UpdateMyProfile(
        Guid actorId,
        Profile profile,
        UpdateProfileRequest request,
        IReadOnlySet<ProfileUpdateField> allowedFields)
    {
        EnsureOwner(actorId, profile);

        var fields = request.Fields ?? InferFields(request);
        if (fields.Contains(ProfileUpdateField.EMAIL) || request.Email is not null)
        {
            throw new ArgumentException("El correo no se puede actualizar mediante el perfil.", nameof(request));
        }

        if (fields.Any(field => !allowedFields.Contains(field)))
        {
            throw new InvalidOperationException("El rol no permite actualizar uno de los campos solicitados.");
        }

        profile.Update(
            fields.Contains(ProfileUpdateField.FIRST_NAME) ? request.FirstName : null,
            fields.Contains(ProfileUpdateField.LAST_NAME) ? request.LastName : null,
            fields.Contains(ProfileUpdateField.PHONE) ? request.Phone : profile.Phone);

        return ToResponse(profile);
    }

    private static void EnsureOwner(Guid actorId, Profile profile)
    {
        if (actorId != profile.UserId)
        {
            throw new UnauthorizedAccessException("Solo el titular puede operar sobre su perfil.");
        }
    }

    private static IReadOnlySet<ProfileUpdateField> InferFields(UpdateProfileRequest request)
    {
        var fields = new HashSet<ProfileUpdateField>();
        if (request.FirstName is not null)
        {
            fields.Add(ProfileUpdateField.FIRST_NAME);
        }

        if (request.LastName is not null)
        {
            fields.Add(ProfileUpdateField.LAST_NAME);
        }

        if (request.Phone is not null)
        {
            fields.Add(ProfileUpdateField.PHONE);
        }

        return fields;
    }

    private static ProfileResponse ToResponse(Profile profile) =>
        new(profile.Id, profile.UserId, profile.FirstName, profile.LastName, profile.Phone);
}
