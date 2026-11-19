namespace Uvoluntapp.Backend.Application.DTOs.Common;

public sealed record PaginationMeta(int Page, int PageSize, int TotalCount, int TotalPages);
