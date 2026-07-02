namespace Onboarding.Application.Proposals;

public sealed record CreateProposalCommand(
    string PartnerId,
    string Cnpj,
    string LegalName,
    string Segment,
    string Mcc,
    IReadOnlyList<CreateProposalPartnerCommand> Partners,
    CreateProposalBankAccountCommand BankAccount,
    CreateProposalAddressCommand Address);

public sealed record CreateProposalPartnerCommand(
    string Name,
    string Cpf,
    decimal ParticipationPercentage,
    bool IsLegalRepresentative);

public sealed record CreateProposalBankAccountCommand(
    string Ispb,
    string Agency,
    string AccountNumber,
    string AccountDigit,
    string AccountType);

public sealed record CreateProposalAddressCommand(
    string ZipCode,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State);

public sealed record UploadDocumentCommand(
    string DocumentType,
    string FileName,
    string ContentType,
    long Length,
    Stream Content);
