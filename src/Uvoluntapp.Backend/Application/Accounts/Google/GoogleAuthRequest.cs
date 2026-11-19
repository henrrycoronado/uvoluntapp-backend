using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Google;

public sealed record GoogleAuthRequest(
    [property: Required] string IdToken);
