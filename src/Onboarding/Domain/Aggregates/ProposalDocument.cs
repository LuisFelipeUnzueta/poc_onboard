using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates;

public sealed class ProposalDocument
{
    public ProposalDocument(DocumentId id, DocumentType documentType, S3Key s3Key, DateTimeOffset uploadedAt)
    {
        Id = id;
        DocumentType = documentType;
        S3Key = s3Key;
        UploadedAt = uploadedAt;
    }

    public DocumentId Id { get; }
    public DocumentType DocumentType { get; }
    public S3Key S3Key { get; }
    public DateTimeOffset UploadedAt { get; }
}
