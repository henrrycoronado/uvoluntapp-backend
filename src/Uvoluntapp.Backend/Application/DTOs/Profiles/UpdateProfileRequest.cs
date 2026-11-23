using System.ComponentModel.DataAnnotations;
using Uvoluntapp.Backend.Domain.Utils.Constants;

namespace Uvoluntapp.Backend.Application.DTOs.Profiles;

public sealed record UpdateProfileRequest(
    [MaxLength(FieldLimits.PersonName)] string? FirstName = null,
    [MaxLength(FieldLimits.PersonName)] string? LastName = null,
    [MaxLength(FieldLimits.Phone)] string? Phone = null,
    [MaxLength(FieldLimits.Email)] string? Email = null,
    IReadOnlySet<ProfileUpdateField>? Fields = null);
