namespace Onboarding.Domain.Common;

public sealed record DomainError(string Code, string Message)
{
    public static readonly DomainError InvalidStatusTransition = new(
        "INVALID_STATUS_TRANSITION",
        "Invalid proposal status transition.");

    public static readonly DomainError DocumentAlreadyUploaded = new(
        "DOCUMENT_ALREADY_UPLOADED",
        "Document type already uploaded for this proposal.");

    public static readonly DomainError ProposalAlreadyExists = new(
        "PROPOSAL_ALREADY_EXISTS",
        "CNPJ already has an active proposal.");

    public static readonly DomainError ProposalNotFound = new(
        "PROPOSAL_NOT_FOUND",
        "Proposal not found.");

    public static readonly DomainError InvalidProposalStatus = new(
        "INVALID_PROPOSAL_STATUS",
        "Proposal is not in PendingDocuments status.");

    public static readonly DomainError IdempotencyConflict = new(
        "IDEMPOTENCY_CONFLICT",
        "Idempotency-Key was already used with a different payload.");

    public static DomainError Validation(string message) => new("VALIDATION_ERROR", message);
}
