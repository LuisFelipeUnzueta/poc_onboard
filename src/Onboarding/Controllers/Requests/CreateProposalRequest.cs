namespace Onboarding.Controllers.Requests;

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
