using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Recovery;

public sealed record ResetPasswordRequest(
    [property: Required] string Token,
    [property: Required, MinLength(8)] string NewPassword);
