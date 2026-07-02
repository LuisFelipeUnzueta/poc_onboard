using Onboarding.Application.Proposals;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Abstractions;

public interface IProposalRepository
{
    Task<bool> ExistsActiveByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken);
    Task AddAsync(Proposal proposal, CancellationToken cancellationToken);
    Task<Proposal?> GetAggregateByIdAsync(ProposalId proposalId, CancellationToken cancellationToken);
    Task SaveDocumentUploadAsync(Proposal proposal, ProposalDocument document, CancellationToken cancellationToken);
    Task<ProposalDetailsResponse?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken);
}
