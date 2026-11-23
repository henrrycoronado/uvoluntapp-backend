namespace Uvoluntapp.Backend.Domain.Utils.Constants;

/// <summary>Longitudes máximas de los datos de cuenta y perfil que escribe una persona; la web las replica en sus formularios.</summary>
public static class FieldLimits
{
    public const int Email = 256;

    public const int Password = 128;

    public const int PersonName = 100;

    public const int Phone = 30;

    public const int Relationship = 200;
}
