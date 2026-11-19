using Uvoluntapp.Backend.Application.DTOs.Accounts;

namespace Uvoluntapp.Backend.Application.Interfaces.Accounts;

public interface IRegistration
{
    Task RegisterAsync(RegisterRequest request, string ip, CancellationToken cancellationToken);

    Task ResendAsync(string email, string ip, CancellationToken cancellationToken);

    Task<VerifyOtpResponse> VerifyAsync(VerifyOtpRequest request, CancellationToken cancellationToken);

    Task CompleteAsync(CompleteRegistrationRequest request, CancellationToken cancellationToken);
}
