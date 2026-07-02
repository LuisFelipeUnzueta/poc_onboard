using Onboarding.Application.Common;

namespace Onboarding.Application.Proposals;

public interface ICreateProposalUseCase
{
    Task<ApplicationResult<CreateProposalResponse>> ExecuteAsync(
        CreateProposalCommand command,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken);
}

public interface IGetProposalUseCase
{
    Task<ApplicationResult<ProposalDetailsResponse>> ExecuteAsync(string proposalId, CancellationToken cancellationToken);
}

public interface IUploadDocumentUseCase
{
    Task<ApplicationResult<UploadDocumentResponse>> ExecuteAsync(
        string proposalId,
        UploadDocumentCommand command,
        CancellationToken cancellationToken);
}
