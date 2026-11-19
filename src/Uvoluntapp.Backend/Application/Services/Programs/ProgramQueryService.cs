using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.DTOs.Programs;
using Uvoluntapp.Backend.Application.Exceptions.Common;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Application.Services.Administration;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Programs;

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
