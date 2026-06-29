namespace Onboarding.Domain.Common;

public sealed record DomainError(string Code, string Message)
{
    public static readonly DomainError InvalidStatusTransition = new(
        "INVALID_STATUS_TRANSITION",
        "Invalid proposal status transition.");

    public static readonly DomainError DocumentAlreadyUploaded = new(
        "DOCUMENT_ALREADY_UPLOADED",
        "Document type already uploaded for this proposal.");

    public static DomainError Validation(string message) => new("VALIDATION_ERROR", message);
}
