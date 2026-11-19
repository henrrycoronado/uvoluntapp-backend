using Uvoluntapp.Backend.Application.Accounts.Contracts;

namespace Uvoluntapp.Backend.Application.Accounts;

public interface IRegistration
{
    Task RegisterAsync(RegisterRequest request, string ip, CancellationToken cancellationToken);

    Task ResendAsync(string email, string ip, CancellationToken cancellationToken);

    Task<VerifyOtpResponse> VerifyAsync(VerifyOtpRequest request, CancellationToken cancellationToken);

    Task CompleteAsync(CompleteRegistrationRequest request, CancellationToken cancellationToken);
}
