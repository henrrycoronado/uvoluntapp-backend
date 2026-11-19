using Uvoluntapp.Backend.Application.Administration.Contracts;

namespace Uvoluntapp.Backend.Application.Profiles.Contracts;

public sealed record ChangeEmailRequest(string NewEmail, string Password);
