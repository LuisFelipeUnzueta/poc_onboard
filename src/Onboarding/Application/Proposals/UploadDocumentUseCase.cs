using System.Text.RegularExpressions;
using Onboarding.Application.Abstractions;
using Onboarding.Application.Common;
using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Proposals;

public sealed partial class UploadDocumentUseCase(
    IProposalRepository proposalRepository,
    IDocumentStorage documentStorage,
    IDocumentRulesService documentRulesService) : IUploadDocumentUseCase
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];

    public async Task<ApplicationResult<UploadDocumentResponse>> ExecuteAsync(
        string proposalId,
        UploadDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var parsedProposalId = ProposalId.Create(proposalId);
        if (parsedProposalId.IsFailure)
        {
            return Failure<UploadDocumentResponse>(DomainError.ProposalNotFound, StatusCodes.Status404NotFound);
        }

        if (!Enum.TryParse<DocumentType>(command.DocumentType, ignoreCase: true, out var documentType))
        {
            return ApplicationResult<UploadDocumentResponse>.Failure(new ApplicationError(
                "VALIDATION_ERROR",
                "DocumentType must be valid.",
                StatusCodes.Status400BadRequest));
        }

        if (!AllowedContentTypes.Contains(command.ContentType))
        {
            return ApplicationResult<UploadDocumentResponse>.Failure(new ApplicationError(
                "INVALID_FILE_TYPE",
                "File type is not accepted.",
                StatusCodes.Status400BadRequest));
        }

        if (command.Length > MaxFileSize)
        {
            return ApplicationResult<UploadDocumentResponse>.Failure(new ApplicationError(
                "FILE_TOO_LARGE",
                "File exceeds 10 MB.",
                StatusCodes.Status400BadRequest));
        }

        var proposal = await proposalRepository.GetAggregateByIdAsync(parsedProposalId.Value!, cancellationToken);
        if (proposal is null)
        {
            return Failure<UploadDocumentResponse>(DomainError.ProposalNotFound, StatusCodes.Status404NotFound);
        }

        if (proposal.Status != ProposalStatus.PendingDocuments)
        {
            return Failure<UploadDocumentResponse>(DomainError.InvalidProposalStatus, StatusCodes.Status422UnprocessableEntity);
        }

        var attachResult = proposal.AttachDocument(documentType, CreateS3Key(parsedProposalId.Value!.Value, documentType, command.FileName));
        if (attachResult.IsFailure)
        {
            return Failure<UploadDocumentResponse>(attachResult.Error!, StatusCodes.Status409Conflict);
        }

        if (documentRulesService.AreAllRequiredDocumentsUploaded(proposal))
        {
            var transitionResult = proposal.TransitionTo(ProposalStatus.WaitingDocumentsApproval);
            if (transitionResult.IsFailure)
            {
                return Failure<UploadDocumentResponse>(transitionResult.Error!, StatusCodes.Status422UnprocessableEntity);
            }

            proposal.MarkDocumentsCompleted();
        }

        var uploadedDocument = proposal.Documents.Single(document => document.DocumentType == documentType);

        await documentStorage.UploadAsync(uploadedDocument.S3Key, command.Content, command.ContentType, cancellationToken);

        try
        {
            await proposalRepository.SaveDocumentUploadAsync(proposal, uploadedDocument, cancellationToken);
        }
        catch (DocumentAlreadyUploadedException)
        {
            await DeleteUploadedObjectAsync(uploadedDocument.S3Key, cancellationToken);
            return Failure<UploadDocumentResponse>(DomainError.DocumentAlreadyUploaded, StatusCodes.Status409Conflict);
        }
        catch
        {
            await DeleteUploadedObjectAsync(uploadedDocument.S3Key, cancellationToken);
            throw;
        }

        return ApplicationResult<UploadDocumentResponse>.Success(new UploadDocumentResponse(
            uploadedDocument.Id.Value,
            uploadedDocument.DocumentType.ToString(),
            "Received",
            uploadedDocument.UploadedAt));
    }

    private async Task DeleteUploadedObjectAsync(S3Key s3Key, CancellationToken cancellationToken)
    {
        try
        {
            await documentStorage.DeleteAsync(s3Key, cancellationToken);
        }
        catch
        {
        }
    }

    private static S3Key CreateS3Key(string proposalId, DocumentType documentType, string fileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var sanitizedFileName = FileNamePattern().Replace(Path.GetFileName(fileName), "-").Trim('-');
        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            sanitizedFileName = "document";
        }

        return S3Key.Create($"proposals/{proposalId}/documents/{documentType}/{timestamp}-{sanitizedFileName}").Value!;
    }

    private static ApplicationResult<T> Failure<T>(DomainError error, int statusCode) =>
        ApplicationResult<T>.Failure(ApplicationError.FromDomain(error, statusCode));

    [GeneratedRegex("[^a-zA-Z0-9._-]+")]
    private static partial Regex FileNamePattern();
}
