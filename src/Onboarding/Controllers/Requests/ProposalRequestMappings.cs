using Onboarding.Application.Proposals;

namespace Onboarding.Controllers.Requests;

public static class ProposalRequestMappings
{
    public static CreateProposalCommand ToCommand(this CreateProposalRequest request) =>
        new(
            request.PartnerId,
            request.Cnpj,
            request.LegalName,
            request.Segment,
            request.Mcc,
            request.Partners.Select(partner => new CreateProposalPartnerCommand(
                partner.Name,
                partner.Cpf,
                partner.ParticipationPercentage,
                partner.IsLegalRepresentative)).ToArray(),
            new CreateProposalBankAccountCommand(
                request.BankAccount.Ispb,
                request.BankAccount.Agency,
                request.BankAccount.AccountNumber,
                request.BankAccount.AccountDigit,
                request.BankAccount.AccountType),
            new CreateProposalAddressCommand(
                request.Address.ZipCode,
                request.Address.Street,
                request.Address.Number,
                request.Address.Complement,
                request.Address.Neighborhood,
                request.Address.City,
                request.Address.State));

    public static UploadDocumentCommand ToCommand(this UploadDocumentFormRequest request, Stream content)
    {
        var file = request.File!;

        return new UploadDocumentCommand(
            request.DocumentType,
            file.FileName,
            file.ContentType,
            file.Length,
            content);
    }
}
