namespace Uvoluntapp.Backend.Application.Common;

public sealed record PaginationMeta(int Page, int PageSize, int TotalCount, int TotalPages);
