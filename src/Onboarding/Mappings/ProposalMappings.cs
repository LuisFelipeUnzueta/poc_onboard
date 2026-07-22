using Mapster;
using Onboarding.Application.Proposals;
using Onboarding.Controllers.Requests;

namespace Onboarding.Mappings;

public sealed class ProposalMappings : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProposalRequest, CreateProposalCommand>()
            .Map(dest => dest.PartnerId, src => src.PartnerId)
            .Map(dest => dest.Cnpj, src => src.Cnpj)
            .Map(dest => dest.LegalName, src => src.LegalName)
            .Map(dest => dest.Segment, src => src.Segment)
            .Map(dest => dest.Mcc, src => src.Mcc)
            .Map(dest => dest.Partners, src => src.Partners)
            .Map(dest => dest.BankAccount, src => src.BankAccount)
            .Map(dest => dest.Address, src => src.Address);

        config.NewConfig<CreateProposalPartnerRequest, CreateProposalPartnerCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Cpf, src => src.Cpf)
            .Map(dest => dest.ParticipationPercentage, src => src.ParticipationPercentage)
            .Map(dest => dest.IsLegalRepresentative, src => src.IsLegalRepresentative);

        config.NewConfig<CreateProposalBankAccountRequest, CreateProposalBankAccountCommand>()
            .Map(dest => dest.Ispb, src => src.Ispb)
            .Map(dest => dest.Agency, src => src.Agency)
            .Map(dest => dest.AccountNumber, src => src.AccountNumber)
            .Map(dest => dest.AccountDigit, src => src.AccountDigit)
            .Map(dest => dest.AccountType, src => src.AccountType);

        config.NewConfig<CreateProposalAddressRequest, CreateProposalAddressCommand>()
            .Map(dest => dest.ZipCode, src => src.ZipCode)
            .Map(dest => dest.Street, src => src.Street)
            .Map(dest => dest.Number, src => src.Number)
            .Map(dest => dest.Complement, src => src.Complement)
            .Map(dest => dest.Neighborhood, src => src.Neighborhood)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.State, src => src.State);
    }
}
