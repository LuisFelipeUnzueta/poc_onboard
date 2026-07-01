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
    IDocumentRulesService documentRulesService) : ICreateProposalUseCase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);

    public async Task<ApplicationResult<CreateProposalResponse>> ExecuteAsync(
        CreateProposalRequest request,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken)
    {
        var cached = await idempotencyStore.GetAsync(idempotencyKey, cancellationToken);
        if (cached is not null)
        {
            if (!string.Equals(cached.RequestHash, requestHash, StringComparison.Ordinal))
            {
                return Failure<CreateProposalResponse>(DomainError.IdempotencyConflict, StatusCodes.Status409Conflict);
            }

            var cachedResponse = JsonSerializer.Deserialize<CreateProposalResponse>(cached.Body, JsonOptions);
            return ApplicationResult<CreateProposalResponse>.Success(cachedResponse!, idempotencyReplayed: true);
        }

        var proposalResult = CreateProposal(request);
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
        }
        catch (ProposalAlreadyExistsException)
        {
            return Failure<CreateProposalResponse>(DomainError.ProposalAlreadyExists, StatusCodes.Status409Conflict);
        }

        var response = new CreateProposalResponse(
            proposal.Id.Value,
            proposal.Status.ToString(),
            documentRulesService.GetRequiredDocuments(proposal.Segment).Select(document => document.ToString()).ToArray(),
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

    private static Result<Proposal> CreateProposal(CreateProposalRequest request)
    {
        if (!Enum.TryParse<Segment>(request.Segment, ignoreCase: true, out var segment))
        {
            return Result<Proposal>.Failure(DomainError.Validation("Segment must be valid."));
        }

        if (!Enum.TryParse<BankAccountType>(request.BankAccount.AccountType, ignoreCase: true, out var accountType))
        {
            return Result<Proposal>.Failure(DomainError.Validation("BankAccount accountType must be valid."));
        }

        var partnerId = PartnerId.Create(request.PartnerId);
        var cnpj = Cnpj.Create(request.Cnpj);
        var legalName = LegalName.Create(request.LegalName);
        var mcc = Mcc.Create(request.Mcc);
        var bankAccount = BankAccount.Create(
            request.BankAccount.Ispb,
            request.BankAccount.Agency,
            request.BankAccount.AccountNumber,
            request.BankAccount.AccountDigit,
            accountType);
        var address = Address.Create(
            request.Address.ZipCode,
            request.Address.Street,
            request.Address.Number,
            request.Address.Complement,
            request.Address.Neighborhood,
            request.Address.City,
            request.Address.State);

        var validationError = FirstError(partnerId.Error, cnpj.Error, legalName.Error, mcc.Error, bankAccount.Error, address.Error);
        if (validationError is not null)
        {
            return Result<Proposal>.Failure(validationError);
        }

        var partners = new List<Partner>();
        foreach (var partnerRequest in request.Partners)
        {
            var cpf = Cpf.Create(partnerRequest.Cpf);
            if (cpf.IsFailure)
            {
                return Result<Proposal>.Failure(cpf.Error!);
            }

            var partner = Partner.Create(
                partnerRequest.Name,
                cpf.Value!,
                partnerRequest.ParticipationPercentage,
                partnerRequest.IsLegalRepresentative);

            if (partner.IsFailure)
            {
                return Result<Proposal>.Failure(partner.Error!);
            }

            partners.Add(partner.Value!);
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
