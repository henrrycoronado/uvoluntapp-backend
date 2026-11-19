using Uvoluntapp.Backend.Application.DTOs.Accounts;
using Uvoluntapp.Backend.Application.Exceptions.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Application.Interfaces.Common;
using Uvoluntapp.Backend.Domain.Entities.Accounts;

namespace Uvoluntapp.Backend.Application.Services.Accounts;

public sealed class RecoveryService(
    IRecoveryAccountStore accounts,
    IRecoveryTokenGenerator tokens,
    IAccountRateLimiter limits,
    IEmailSender emailSender,
    IWebLinks links,
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
                $"Usa este enlace para crear una contraseña nueva. Vence en 30 minutos y sirve una sola vez: {links.PasswordReset(generated.RawToken)}",
                cancellationToken);
        }

        return new MessageResponse { Message = GenericMessage };
    }

    public async Task<MessageResponse> ResetAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var hash = tokens.Hash(request.Token);
        var token = await accounts.FindTokenAsync(hash, cancellationToken);
        if (token is null || !token.TryConsume(clock.GetUtcNow().UtcDateTime))
        {
            throw new AccountException(AccountError.InvalidCode);
        }

        // Revoca todas las sesiones de la cuenta y deja el evento en la auditoría.
        await accounts.SetPasswordAsync(token.UserId, request.NewPassword, clock.GetUtcNow().UtcDateTime, cancellationToken);
        return new MessageResponse { Message = "Contraseña restablecida. Inicia sesión con la nueva." };
    }
}
