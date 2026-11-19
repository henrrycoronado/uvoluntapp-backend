namespace Uvoluntapp.Backend.Application.Common;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }

    public ForbiddenException()
        : base()
    {
    }

    public ForbiddenException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
