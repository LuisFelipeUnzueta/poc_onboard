using FluentAssertions;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;
using Onboarding.Domain.Events;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.UnitTests.Domain;

public sealed class ProposalTests
{
    [Fact]
    public void Create_Should_Return_Success_When_Valid()
    {
        var result = CreateProposal();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ProposalStatus.PendingDocuments);
        result.Value.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ProposalCreated);
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Without_Partners()
    {
        var result = Proposal.Create(
            PartnerId.New(),
            ValidCnpj(),
            ValidRazaoSocial(),
            Segment.PayFac,
            ValidMcc(),
            [],
            ValidBankAccount(),
            ValidAddress());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Return_Failure_When_Participation_Is_Not_One_Hundred()
    {
        var partner = Partner.Create("Joao Silva", ValidCpf(), 80, true).Value!;

        var result = Proposal.Create(
            PartnerId.New(),
            ValidCnpj(),
            ValidRazaoSocial(),
            Segment.PayFac,
            ValidMcc(),
            [partner],
            ValidBankAccount(),
            ValidAddress());

        result.IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(ProposalStatus.PendingPricing)]
    [InlineData(ProposalStatus.PendingRiskAnalysis)]
    [InlineData(ProposalStatus.Approved)]
    [InlineData(ProposalStatus.SubmittedToAcquirer)]
    [InlineData(ProposalStatus.Completed)]
    public void TransitionTo_Should_Return_Success_When_Transition_Is_Valid(ProposalStatus nextStatus)
    {
        var proposal = CreateProposal().Value!;
        MoveToPreviousStatus(proposal, nextStatus);

        var result = proposal.TransitionTo(nextStatus);

        result.IsSuccess.Should().BeTrue();
        proposal.Status.Should().Be(nextStatus);
        proposal.DomainEvents.Should().Contain(domainEvent => domainEvent is StatusChanged);
    }

    [Theory]
    [InlineData(ProposalStatus.Approved)]
    [InlineData(ProposalStatus.Completed)]
    [InlineData(ProposalStatus.Rejected)]
    public void TransitionTo_Should_Return_Failure_When_Transition_Is_Invalid(ProposalStatus invalidStatus)
    {
        var proposal = CreateProposal().Value!;

        var result = proposal.TransitionTo(invalidStatus);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainError.InvalidStatusTransition);
        proposal.Status.Should().Be(ProposalStatus.PendingDocuments);
    }

    [Fact]
    public void AttachDocument_Should_Add_Document_When_Not_Duplicated()
    {
        var proposal = CreateProposal().Value!;
        var s3Key = ValidS3Key(proposal.Id, DocumentType.CnpjCard);

        var result = proposal.AttachDocument(DocumentType.CnpjCard, s3Key);

        result.IsSuccess.Should().BeTrue();
        proposal.Documents.Should().ContainSingle(document => document.DocumentType == DocumentType.CnpjCard);
        proposal.DomainEvents.Should().Contain(domainEvent => domainEvent is DocumentAttached);
    }

    [Fact]
    public void AttachDocument_Should_Return_Failure_When_Duplicated()
    {
        var proposal = CreateProposal().Value!;
        var firstKey = ValidS3Key(proposal.Id, DocumentType.CnpjCard);
        var secondKey = ValidS3Key(proposal.Id, DocumentType.CnpjCard, "file-2.pdf");

        proposal.AttachDocument(DocumentType.CnpjCard, firstKey);

        var result = proposal.AttachDocument(DocumentType.CnpjCard, secondKey);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainError.DocumentAlreadyUploaded);
        proposal.Documents.Should().ContainSingle();
    }

    private static Result<Proposal> CreateProposal()
    {
        var partner = Partner.Create("Joao Silva", ValidCpf(), 100, true).Value!;

        return Proposal.Create(
            PartnerId.New(),
            ValidCnpj(),
            ValidRazaoSocial(),
            Segment.PayFac,
            ValidMcc(),
            [partner],
            ValidBankAccount(),
            ValidAddress());
    }

    private static void MoveToPreviousStatus(Proposal proposal, ProposalStatus nextStatus)
    {
        var path = nextStatus switch
        {
            ProposalStatus.PendingPricing => Array.Empty<ProposalStatus>(),
            ProposalStatus.PendingRiskAnalysis => [ProposalStatus.PendingPricing],
            ProposalStatus.Approved => [ProposalStatus.PendingPricing, ProposalStatus.PendingRiskAnalysis],
            ProposalStatus.SubmittedToAcquirer => [ProposalStatus.PendingPricing, ProposalStatus.PendingRiskAnalysis, ProposalStatus.Approved],
            ProposalStatus.Completed => [ProposalStatus.PendingPricing, ProposalStatus.PendingRiskAnalysis, ProposalStatus.Approved, ProposalStatus.SubmittedToAcquirer],
            _ => Array.Empty<ProposalStatus>()
        };

        foreach (var status in path)
        {
            proposal.TransitionTo(status);
        }
    }

    private static Cnpj ValidCnpj() => Cnpj.Create("11.444.777/0001-61").Value!;

    private static Cpf ValidCpf() => Cpf.Create("529.982.247-25").Value!;

    private static RazaoSocial ValidRazaoSocial() => RazaoSocial.Create("Empresa Exemplo Ltda").Value!;

    private static Mcc ValidMcc() => Mcc.Create("5411").Value!;

    private static BankAccount ValidBankAccount()
    {
        return BankAccount.Create("60746948", "0001", "123456", "7", BankAccountType.CheckingAccount).Value!;
    }

    private static Address ValidAddress()
    {
        return Address.Create(
            "01310-100",
            "Av. Paulista",
            "1000",
            "Sala 101",
            "Bela Vista",
            "Sao Paulo",
            "SP").Value!;
    }

    private static S3Key ValidS3Key(
        ProposalId proposalId,
        DocumentType documentType,
        string fileName = "file.pdf")
    {
        return S3Key.Create($"proposals/{proposalId.Value}/documents/{documentType}/{fileName}").Value!;
    }
}
