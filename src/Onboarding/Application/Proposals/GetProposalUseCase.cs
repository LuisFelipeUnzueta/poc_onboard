using Onboarding.Application.Abstractions;
using Onboarding.Application.Common;
using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Proposals;

public sealed class GetProposalUseCase(IProposalRepository proposalRepository) : IGetProposalUseCase
{
    public async Task<ApplicationResult<ProposalDetailsResponse>> ExecuteAsync(
        string proposalId,
        CancellationToken cancellationToken)
    {
        var id = ProposalId.Create(proposalId);
        if (id.IsFailure)
        {
            return Failure(id.Error!, StatusCodes.Status400BadRequest);
        }

        var proposal = await proposalRepository.GetByIdAsync(id.Value!, cancellationToken);

        return proposal is null
            ? Failure(DomainError.ProposalNotFound, StatusCodes.Status404NotFound)
            : ApplicationResult<ProposalDetailsResponse>.Success(proposal);
    }

    private static ApplicationResult<ProposalDetailsResponse> Failure(DomainError error, int statusCode) =>
        ApplicationResult<ProposalDetailsResponse>.Failure(ApplicationError.FromDomain(error, statusCode));
}
