namespace Uvoluntapp.Backend.Application.Interfaces.Common;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
