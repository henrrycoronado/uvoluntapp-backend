namespace Uvoluntapp.Backend.Application.Audit;

public sealed record PaginationMeta(int Page, int PageSize, int TotalCount, int TotalPages);
