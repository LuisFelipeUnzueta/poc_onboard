using Onboarding.Domain.Enums;

namespace Onboarding.Application.Proposals;

public sealed record CreateProposalRequest(
    string PartnerId,
    string Cnpj,
    string LegalName,
    string Segment,
    string Mcc,
    IReadOnlyList<CreateProposalPartnerRequest> Partners,
    CreateProposalBankAccountRequest BankAccount,
    CreateProposalAddressRequest Address);

public sealed record CreateProposalPartnerRequest(
    string Name,
    string Cpf,
    decimal ParticipationPercentage,
    bool IsLegalRepresentative);

public sealed record CreateProposalBankAccountRequest(
    string Ispb,
    string Agency,
    string AccountNumber,
    string AccountDigit,
    string AccountType);

public sealed record CreateProposalAddressRequest(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State);

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

public sealed record UploadDocumentRequest(
    string DocumentType,
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

public sealed record UploadDocumentResponse(
    string DocumentId,
    string DocumentType,
    string Status,
    DateTimeOffset UploadedAt);
