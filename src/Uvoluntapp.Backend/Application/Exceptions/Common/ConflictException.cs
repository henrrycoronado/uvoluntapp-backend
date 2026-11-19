namespace Uvoluntapp.Backend.Application.Exceptions.Common;

public sealed class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException()
        : base()
    {
    }

    public ConflictException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
