using System.ComponentModel.DataAnnotations;

namespace Uvoluntapp.Backend.Application.Accounts.Recovery;

public sealed record ForgotPasswordRequest(
    [property: Required, EmailAddress] string Email);
