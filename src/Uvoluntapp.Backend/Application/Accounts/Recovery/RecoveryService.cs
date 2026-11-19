using System.Security.Cryptography;
using System.Text;
using Uvoluntapp.Backend.Application.Accounts.Contracts;
using Uvoluntapp.Backend.Domain.Accounts;

namespace Uvoluntapp.Backend.Application.Accounts.Recovery;

public sealed class RecoveryService(
    IRecoveryAccountStore accounts,
    IRecoveryTokenGenerator tokens,
    IAccountRateLimiter limits,
    IEmailSender emailSender,
    TimeProvider clock)
{
    private const string GenericMessage = "Si la cuenta existe, recibirás un enlace de recuperación.";

    public async Task<MessageResponse> RequestAsync(
        ForgotPasswordRequest request,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var email = request.Email.Trim().ToUpperInvariant();
        await limits.CheckAsync("password-recovery", email, ipAddress, cancellationToken);
        var userId = await accounts.FindUserIdByEmailAsync(email, cancellationToken);
        if (userId is not null)
        {
            var generated = tokens.Create();
            await accounts.SaveTokenAsync(
                PasswordRecoveryToken.Create(userId.Value, generated.TokenHash, clock.GetUtcNow().UtcDateTime),
                cancellationToken);
            await emailSender.SendAsync(
                request.Email.Trim(),
                "Recuperación de contraseña",
                $"Usa este enlace para recuperar tu contraseña: {generated.RawToken}",
                cancellationToken);
        }

        return new MessageResponse { Message = GenericMessage };
    }

    public async Task<MessageResponse> ResetAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));
        var token = await accounts.FindTokenAsync(hash, cancellationToken);
        if (token is null || !token.TryConsume(clock.GetUtcNow().UtcDateTime))
        {
            throw new AccountException(AccountError.InvalidCode);
        }

        await accounts.SetPasswordAsync(token.UserId, request.NewPassword, cancellationToken);
        return new MessageResponse { Message = "Contraseña restablecida." };
    }
}
