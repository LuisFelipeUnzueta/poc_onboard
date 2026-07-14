using System.Text.Json;
using Onboarding.Application.Abstractions;
using Onboarding.Application.Common;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Proposals;

public sealed class CreateProposalUseCase(
    IProposalRepository proposalRepository,
    IIdempotencyStore idempotencyStore,
    IDocumentRulesService documentRulesService,
    IOnboardingMetrics metrics) : ICreateProposalUseCase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);

    public async Task<ApplicationResult<CreateProposalResponse>> ExecuteAsync(
        CreateProposalCommand command,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var cached = await idempotencyStore.GetAsync(idempotencyKey, cancellationToken);
        if (cached is not null)
        {
            metrics.IdempotencyCacheHit("create_proposal");

            if (!string.Equals(cached.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return Failure<CreateProposalResponse>(DomainError.IdempotencyConflict, StatusCodes.Status409Conflict);
            }

            var cachedResponse = JsonSerializer.Deserialize<CreateProposalResponse>(cached.Body, JsonOptions);
            return ApplicationResult<CreateProposalResponse>.Success(cachedResponse!, idempotencyReplayed: true);
        }

        var proposalResult = CreateProposal(command);
        if (proposalResult.IsFailure)
        {
            return Failure<CreateProposalResponse>(proposalResult.Error!, StatusCodes.Status400BadRequest);
        }

        var proposal = proposalResult.Value!;
        if (await proposalRepository.ExistsActiveByCnpjAsync(proposal.Cnpj, cancellationToken))
        {
            return Failure<CreateProposalResponse>(DomainError.ProposalAlreadyExists, StatusCodes.Status409Conflict);
        }

        try
        {
            await proposalRepository.AddAsync(proposal, cancellationToken);
            metrics.ProposalCreated(proposal.Segment);
        }
        catch (ProposalAlreadyExistsException)
        {
            return Failure<CreateProposalResponse>(DomainError.ProposalAlreadyExists, StatusCodes.Status409Conflict);
        }

        var response = new CreateProposalResponse(
            proposal.Id.Value,
            proposal.Status.ToString(),
            documentRulesService.GetRequiredDocuments(proposal.Segment, PersonType.LegalPerson).Select(document => document.ToString()).ToArray(),
            proposal.CreatedAt);

        await idempotencyStore.SetAsync(
            idempotencyKey,
            new IdempotencyEntry(StatusCodes.Status201Created, JsonSerializer.Serialize(response, JsonOptions), requestHash),
            IdempotencyTtl,
            cancellationToken);

        return ApplicationResult<CreateProposalResponse>.Success(response);
    }

    private static ApplicationResult<T> Failure<T>(DomainError error, int statusCode) =>
        ApplicationResult<T>.Failure(ApplicationError.FromDomain(error, statusCode));

    private static Result<Proposal> CreateProposal(CreateProposalCommand command)
    {
        if (!Enum.TryParse<Segment>(command.Segment, ignoreCase: true, out var segment))
        {
            return Result<Proposal>.Failure(DomainError.Validation("Segment must be valid."));
        }

        if (!Enum.TryParse<BankAccountType>(command.BankAccount.AccountType, ignoreCase: true, out var accountType))
        {
            return Result<Proposal>.Failure(DomainError.Validation("BankAccount accountType must be valid."));
        }

        var partnerId = PartnerId.Create(command.PartnerId);
        var cnpj = Cnpj.Create(command.Cnpj);
        var legalName = LegalName.Create(command.LegalName);
        var mcc = Mcc.Create(command.Mcc);
        var bankAccount = BankAccount.Create(
            command.BankAccount.Ispb,
            command.BankAccount.Agency,
            command.BankAccount.AccountNumber,
            command.BankAccount.AccountDigit,
            accountType);
        var address = Address.Create(
            command.Address.ZipCode,
            command.Address.Street,
            command.Address.Number,
            command.Address.Complement,
            command.Address.Neighborhood,
            command.Address.City,
            command.Address.State);

        var validationError = FirstError(partnerId.Error, cnpj.Error, legalName.Error, mcc.Error, bankAccount.Error, address.Error);
        if (validationError is not null)
        {
            return Result<Proposal>.Failure(validationError);
        }

        var partners = new List<Partner>();
        foreach (var partner in command.Partners)
        {
            var cpf = Cpf.Create(partner.Cpf);
            if (cpf.IsFailure)
            {
                return Result<Proposal>.Failure(cpf.Error!);
            }

            var proposalPartner = Partner.Create(
                partner.Name,
                cpf.Value!,
                partner.ParticipationPercentage,
                partner.IsLegalRepresentative);

            if (proposalPartner.IsFailure)
            {
                return Result<Proposal>.Failure(proposalPartner.Error!);
            }

            partners.Add(proposalPartner.Value!);
        }

        return Proposal.Create(
            partnerId.Value!,
            cnpj.Value!,
            legalName.Value!,
            segment,
            mcc.Value!,
            partners,
            bankAccount.Value!,
            address.Value!);
    }

    private static DomainError? FirstError(params DomainError?[] errors) => errors.FirstOrDefault(error => error is not null);
}
