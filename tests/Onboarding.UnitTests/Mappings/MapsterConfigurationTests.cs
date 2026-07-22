using FluentAssertions;
using Mapster;
using Onboarding.Application.Proposals;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;
using Onboarding.Mappings;

namespace Onboarding.UnitTests.Mappings;

public sealed class MapsterConfigurationTests
{
    [Fact]
    public void GlobalSettings_Should_Compile_Without_Errors()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

        var act = () => TypeAdapterConfig.GlobalSettings.Compile();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateProposalRequest_Should_Map_To_CreateProposalCommand()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);
        TypeAdapterConfig.GlobalSettings.Compile();

        var request = new Onboarding.Controllers.Requests.CreateProposalRequest(
            PartnerId.New().Value,
            "12.345.678/0001-99",
            "Empresa Teste Ltda",
            "PayFac",
            "5411",
            [
                new Onboarding.Controllers.Requests.CreateProposalPartnerRequest(
                    "Joao Silva",
                    "529.982.247-25",
                    100,
                    true)
            ],
            new Onboarding.Controllers.Requests.CreateProposalBankAccountRequest(
                "60746948",
                "0001",
                "123456",
                "7",
                "CheckingAccount"),
            new Onboarding.Controllers.Requests.CreateProposalAddressRequest(
                "01310-100",
                "Av. Paulista",
                "1000",
                null,
                "Bela Vista",
                "Sao Paulo",
                "SP"));

        var command = request.Adapt<CreateProposalCommand>();

        command.Should().NotBeNull();
        command.PartnerId.Should().Be(request.PartnerId);
        command.Cnpj.Should().Be(request.Cnpj);
        command.LegalName.Should().Be(request.LegalName);
        command.Segment.Should().Be(request.Segment);
        command.Mcc.Should().Be(request.Mcc);
        command.Partners.Should().HaveCount(1);
        command.Partners[0].Name.Should().Be("Joao Silva");
        command.Partners[0].Cpf.Should().Be("529.982.247-25");
        command.Partners[0].ParticipationPercentage.Should().Be(100);
        command.Partners[0].IsLegalRepresentative.Should().BeTrue();
        command.BankAccount.Ispb.Should().Be("60746948");
        command.BankAccount.Agency.Should().Be("0001");
        command.BankAccount.AccountNumber.Should().Be("123456");
        command.BankAccount.AccountDigit.Should().Be("7");
        command.BankAccount.AccountType.Should().Be("CheckingAccount");
        command.Address.ZipCode.Should().Be("01310-100");
        command.Address.Street.Should().Be("Av. Paulista");
        command.Address.Number.Should().Be("1000");
        command.Address.Complement.Should().BeNull();
        command.Address.Neighborhood.Should().Be("Bela Vista");
        command.Address.City.Should().Be("Sao Paulo");
        command.Address.State.Should().Be("SP");
    }

    [Fact]
    public void Proposal_Should_Map_To_ProposalDetailsResponse()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);
        TypeAdapterConfig.GlobalSettings.Compile();

        var proposal = Proposal.Create(
            PartnerId.New(),
            Cnpj.Create("11.222.333/0001-81").Value!,
            LegalName.Create("Empresa Teste Ltda").Value!,
            Segment.PayFac,
            Mcc.Create("5411").Value!,
            [Partner.Create("Joao Silva", Cpf.Create("529.982.247-25").Value!, 100, true).Value!],
            BankAccount.Create("60746948", "0001", "123456", "7", BankAccountType.CheckingAccount).Value!,
            Address.Create("01310-100", "Av. Paulista", "1000", null, "Bela Vista", "Sao Paulo", "SP").Value!).Value!;

        var response = proposal.ToDetailsResponse();

        response.Should().NotBeNull();
        response.ProposalId.Should().Be(proposal.Id.Value);
        response.Status.Should().Be(proposal.Status.ToString());
        response.Cnpj.Should().Be("11222333000181");
        response.LegalName.Should().Be("Empresa Teste Ltda");
        response.Segment.Should().Be("PayFac");
        response.CreatedAt.Should().Be(proposal.CreatedAt);
        response.UpdatedAt.Should().Be(proposal.UpdatedAt);
    }
}
