namespace Uvoluntapp.Backend.Application.Exceptions.Activities;

public sealed class EnrollmentException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
