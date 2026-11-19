using Uvoluntapp.Backend.Application.DTOs.Common;

namespace Uvoluntapp.Backend.Application.DTOs.Administration;

public sealed record PaginatedItems<T>(IReadOnlyList<T> Items, PaginationMeta Meta);
