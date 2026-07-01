using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;
using Onboarding.Domain.Events;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates;

public sealed class Proposal : AggregateRoot<ProposalId>
{
    private readonly List<Partner> _partners = [];
    private readonly List<ProposalDocument> _documents = [];

    private Proposal(
        ProposalId id,
        PartnerId partnerId,
        Cnpj cnpj,
        LegalName legalName,
        Segment segment,
        Mcc mcc,
        IEnumerable<Partner> partners,
        BankAccount bankAccount,
        Address address,
        DateTimeOffset createdAt)
        : base(id)
    {
        PartnerId = partnerId;
        Cnpj = cnpj;
        LegalName = legalName;
        Segment = segment;
        Mcc = mcc;
        Status = ProposalStatus.PendingDocuments;
        _partners.AddRange(partners);
        BankAccount = bankAccount;
        Address = address;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Version = 1;
    }

    public PartnerId PartnerId { get; private set; }
    public Cnpj Cnpj { get; private set; }
    public LegalName LegalName { get; private set; }
    public Segment Segment { get; private set; }
    public Mcc Mcc { get; private set; }
    public ProposalStatus Status { get; private set; }
    public IReadOnlyList<Partner> Partners => _partners.AsReadOnly();
    public BankAccount BankAccount { get; private set; }
    public Address Address { get; private set; }
    public IReadOnlyList<ProposalDocument> Documents => _documents.AsReadOnly();
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public int Version { get; private set; }

    public static Result<Proposal> Create(
        PartnerId partnerId,
        Cnpj cnpj,
        LegalName legalName,
        Segment segment,
        Mcc mcc,
        IEnumerable<Partner> partners,
        BankAccount bankAccount,
        Address address)
    {
        var partnersList = partners.ToList();

        if (partnersList.Count == 0)
        {
            return Result<Proposal>.Failure(DomainError.Validation("Proposal must have at least one partner."));
        }

        if (partnersList.Sum(partner => partner.ParticipationPercentage) != 100)
        {
            return Result<Proposal>.Failure(DomainError.Validation("Partners participation must total 100."));
        }

        if (!partnersList.Any(partner => partner.IsLegalRepresentative))
        {
            return Result<Proposal>.Failure(DomainError.Validation("Proposal must have a legal representative."));
        }

        var proposal = new Proposal(
            ProposalId.New(),
            partnerId,
            cnpj,
            legalName,
            segment,
            mcc,
            partnersList,
            bankAccount,
            address,
            DateTimeOffset.UtcNow);

        proposal.Apply(new ProposalCreated(proposal.Id, proposal.CreatedAt));

        return Result<Proposal>.Success(proposal);
    }

    public Result AttachDocument(DocumentType documentType, S3Key s3Key)
    {
        if (_documents.Any(document => document.DocumentType == documentType))
        {
            return Result.Failure(DomainError.DocumentAlreadyUploaded);
        }

        var domainEvent = new DocumentAttached(Id, documentType, s3Key, DateTimeOffset.UtcNow);
        Apply(domainEvent);

        return Result.Success();
    }

    public Result TransitionTo(ProposalStatus newStatus)
    {
        if (Status == newStatus)
        {
            return Result.Success();
        }

        if (!CanTransitionTo(newStatus))
        {
            return Result.Failure(DomainError.InvalidStatusTransition);
        }

        Apply(new StatusChanged(Id, Status, newStatus, DateTimeOffset.UtcNow));

        return Result.Success();
    }

    private bool CanTransitionTo(ProposalStatus newStatus)
    {
        return Status switch
        {
            ProposalStatus.Draft => newStatus == ProposalStatus.PendingDocuments,
            ProposalStatus.PendingDocuments => newStatus == ProposalStatus.PendingPricing,
            ProposalStatus.PendingPricing => newStatus == ProposalStatus.PendingRiskAnalysis,
            ProposalStatus.PendingRiskAnalysis => newStatus is ProposalStatus.Approved or ProposalStatus.Rejected,
            ProposalStatus.Approved => newStatus == ProposalStatus.SubmittedToAcquirer,
            ProposalStatus.SubmittedToAcquirer => newStatus == ProposalStatus.Completed,
            ProposalStatus.Rejected or ProposalStatus.Completed => false,
            _ => false
        };
    }

    private void Apply(ProposalCreated @event)
    {
        AddDomainEvent(@event);
    }

    private void Apply(DocumentAttached @event)
    {
        _documents.Add(new ProposalDocument(@event.DocumentType, @event.S3Key, @event.OccurredAt));
        Touch(@event.OccurredAt);
        AddDomainEvent(@event);
    }

    private void Apply(StatusChanged @event)
    {
        Status = @event.NewStatus;
        Touch(@event.OccurredAt);
        AddDomainEvent(@event);
    }

    private void Touch(DateTimeOffset updatedAt)
    {
        UpdatedAt = updatedAt;
        Version++;
    }
}
