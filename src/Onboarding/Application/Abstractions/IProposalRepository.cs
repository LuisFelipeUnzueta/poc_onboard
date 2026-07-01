using Onboarding.Application.Proposals;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Abstractions;

public interface IProposalRepository
{
    Task<bool> ExistsActiveByCnpjAsync(Cnpj cnpj, CancellationToken cancellationToken);
    Task AddAsync(Proposal proposal, CancellationToken cancellationToken);
    Task<ProposalDetailsResponse?> GetByIdAsync(ProposalId proposalId, CancellationToken cancellationToken);
}
