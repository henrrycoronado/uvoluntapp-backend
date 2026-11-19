namespace Uvoluntapp.Backend.Application.Exceptions.Accounts;

public sealed class AccountException(AccountError error, IReadOnlyDictionary<string, object?>? extensions = null) : Exception
{
    public AccountError Error { get; } = error;

    // Datos que la web necesita para guiar a la persona dueña de la cuenta (estado, fecha disponible, motivo).
    public IReadOnlyDictionary<string, object?> Extensions { get; } = extensions ?? new Dictionary<string, object?>();
}
