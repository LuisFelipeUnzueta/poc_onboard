using Onboarding.Domain.Enums;

namespace Onboarding.Application.Proposals;

public sealed record CreateProposalResponse(
    string ProposalId,
    string Status,
    IReadOnlyList<string> RequiredDocuments,
    DateTimeOffset CreatedAt);

public sealed record ProposalDetailsResponse(
    string ProposalId,
    string Status,
    string Cnpj,
    string LegalName,
    string Segment,
    IReadOnlyList<ProposalDocumentResponse> Documents,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProposalDocumentResponse(
    DocumentType DocumentType,
    string Status,
    DateTimeOffset UploadedAt);

public sealed record UploadDocumentResponse(
    string DocumentId,
    string DocumentType,
    string Status,
    DateTimeOffset UploadedAt);
