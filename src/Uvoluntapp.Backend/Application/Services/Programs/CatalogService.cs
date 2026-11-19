using Uvoluntapp.Backend.Application.DTOs.Activities;
using Uvoluntapp.Backend.Application.DTOs.Administration;
using Uvoluntapp.Backend.Application.DTOs.Common;
using Uvoluntapp.Backend.Application.DTOs.Programs;
using Uvoluntapp.Backend.Application.Interfaces.Activities;
using Uvoluntapp.Backend.Application.Interfaces.Programs;
using Uvoluntapp.Backend.Domain.Utils.Enums;

namespace Uvoluntapp.Backend.Application.Services.Programs;

/// <summary>
/// Catálogo público sin sesión (SCRUM-52): programas activos y actividades publicadas o en ejecución
/// de programas activos o libres. Sin datos internos ni enlaces de reunión.
/// </summary>
public sealed class CatalogService(IProgramStore programs, IActivityStore activities, IAcademicPeriodStore periods)
{
    private static readonly ProgramStatus[] _activeOnly = [ProgramStatus.ACTIVE];

    private static readonly ActivityStatus[] _public = [ActivityStatus.PUBLISHED, ActivityStatus.IN_PROGRESS];

    public async Task<PaginatedItems<CatalogProgramSummary>> ProgramsAsync(Guid? periodId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var result = await programs.SearchAsync(_activeOnly, includeArchived: false, periodId, page, pageSize, cancellationToken);
        return new PaginatedItems<CatalogProgramSummary>(
            result.Items.Select(p => new CatalogProgramSummary(p.Id, p.Name, p.Description, p.Contact, p.ReferenceSchedule)).ToList(),
            new PaginationMeta(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<PaginatedItems<CatalogActivitySummary>> ActivitiesAsync(ActivityQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ActivityStatus> statuses = query.Status is { } wanted && _public.Contains(wanted) ? [wanted] : _public;
        var result = await activities.SearchAsync(
            new ActivitySearch(
                query.ProgramId,
                query.PeriodId,
                statuses,
                query.Independent,
                query.GroupLabel,
                query.From,
                query.To,
                AllDrafts: false,
                DraftProgramIds: [],
                DraftActivityIds: [],
                OnlyActivePrograms: true,
                query.Page,
                query.PageSize),
            cancellationToken);
        var names = await programs.NamesAsync(result.Items.Where(a => a.ProgramId is not null).Select(a => a.ProgramId!.Value).Distinct().ToList(), cancellationToken);
        return new PaginatedItems<CatalogActivitySummary>(
            result.Items.Select(a => CatalogActivitySummary.From(a, a.ProgramId is { } id && names.TryGetValue(id, out var name) ? name : null)).ToList(),
            new PaginationMeta(result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    public async Task<IReadOnlyList<AcademicPeriodResponse>> PeriodsAsync(CancellationToken cancellationToken = default) =>
        (await periods.ListAsync(cancellationToken)).Select(AcademicPeriodResponse.From).ToList();
}
