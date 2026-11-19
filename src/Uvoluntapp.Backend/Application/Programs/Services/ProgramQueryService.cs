using Uvoluntapp.Backend.Application.Administration.Contracts;
using Uvoluntapp.Backend.Application.Administration.Services;
using Uvoluntapp.Backend.Application.Common;
using Uvoluntapp.Backend.Application.Programs.Contracts;
using Uvoluntapp.Backend.Application.Programs.Ports;
using Uvoluntapp.Backend.Domain.Programs;

namespace Uvoluntapp.Backend.Application.Programs.Services;

/// <summary>
/// Lectura de programas. El voluntario ve solo programas activos.
/// ADMIN y líderes ven todos como revisores; la gestión es del módulo 2.
/// </summary>
public sealed class ProgramQueryService(AdminAccessGuard guard, IProgramReader programs)
{
    public async Task<PaginatedItems<ProgramResponse>> ListAsync(
        Guid actorId,
        ProgramStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var actor = await guard.ResolveAsync(actorId, cancellationToken);
        if (!actor.IsActive)
        {
            throw new ForbiddenException("Tu cuenta no está activa.");
        }

        var effectiveStatus = actor.CanReview ? status : ProgramStatus.ACTIVE;
        var result = await programs.SearchAsync(effectiveStatus, page, pageSize, cancellationToken);
        return new PaginatedItems<ProgramResponse>(
            result.Items.Select(ProgramResponse.From).ToList(),
            new PaginationMeta(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }
}
