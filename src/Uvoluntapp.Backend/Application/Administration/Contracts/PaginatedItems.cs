using Uvoluntapp.Backend.Application.Common;

namespace Uvoluntapp.Backend.Application.Administration.Contracts;

public sealed record PaginatedItems<T>(IReadOnlyList<T> Items, PaginationMeta Meta);
