using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Accounts;
using Uvoluntapp.Backend.Domain.Entities.Accounts;
using Uvoluntapp.Backend.Infrastructure.Identity;

namespace Uvoluntapp.Backend.Infrastructure.Persistence.Repositories;

public sealed class RecoveryAccountStore(AppDbContext db, UserManager<AppUser> users) : IRecoveryAccountStore
{
    public Task<Guid?> FindUserIdByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.Where(u => u.NormalizedEmail == normalizedEmail).Select(u => (Guid?)u.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task SaveTokenAsync(PasswordRecoveryToken token, CancellationToken cancellationToken)
    {
        await db.PasswordRecoveryTokens.AddAsync(token, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<PasswordRecoveryToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.PasswordRecoveryTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public async Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("La cuenta no existe.");

        // El enlace ya fue validado y consumido por la aplicación; aquí solo se reemplaza el hash.
        var removed = await users.RemovePasswordAsync(user);
        var added = removed.Succeeded ? await users.AddPasswordAsync(user, newPassword) : removed;
        if (!added.Succeeded)
        {
            throw new ArgumentException(string.Join(' ', added.Errors.Select(e => e.Description)), nameof(newPassword));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
