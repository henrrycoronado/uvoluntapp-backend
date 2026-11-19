namespace Uvoluntapp.Backend.Application.Administration.Services;

internal static class Parse
{
    public static Guid ParseGuid(string value, string field) =>
        Guid.TryParse(value, out var parsed) && parsed != Guid.Empty
            ? parsed
            : throw new ArgumentException($"{field} debe ser un identificador válido.", field);

    public static TEnum ParseEnum<TEnum>(string value, string field)
        where TEnum : struct, Enum
        =>
        Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new ArgumentException($"{field} tiene un valor no válido.", field);
}
