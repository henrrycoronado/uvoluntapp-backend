namespace Uvoluntapp.Backend.Domain.Accounts;

public sealed record OneTimeCodeVerificationResult(
    bool IsValid,
    OneTimeCodeRejectionReason? RejectionReason)
{
    public static OneTimeCodeVerificationResult Accepted { get; } = new(true, null);

    public static OneTimeCodeVerificationResult Rejected(OneTimeCodeRejectionReason reason) =>
        new(false, reason);
}
