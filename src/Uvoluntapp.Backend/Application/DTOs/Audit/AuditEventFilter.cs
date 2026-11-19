namespace Uvoluntapp.Backend.Application.DTOs.Audit;

public sealed record AuditEventFilter(
    Guid? ActorId = null,
    string? Action = null,
    string? EntityType = null,
    Guid? EntityId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20)
{
    public const int MaxPageSize = 100;

    public AuditEventFilter Normalize()
    {
        if (ActorId == Guid.Empty)
        {
            throw new ArgumentException("El actor no puede ser un identificador vacío.", nameof(ActorId));
        }

        if (EntityId == Guid.Empty)
        {
            throw new ArgumentException("El recurso no puede ser un identificador vacío.", nameof(EntityId));
        }

        if (Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Page), "La página empieza en 1.");
        }

        if (PageSize is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PageSize),
                $"El tamaño de página debe estar entre 1 y {MaxPageSize}.");
        }

        var from = NormalizeUtc(From);
        var to = NormalizeUtc(To);
        if (from.HasValue && to.HasValue && from > to)
        {
            throw new ArgumentException("El inicio del rango no puede ser posterior al fin.");
        }

        return this with
        {
            Action = NullIfBlank(Action),
            EntityType = NullIfBlank(EntityType),
            From = from,
            To = to,
        };
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
    }
}
