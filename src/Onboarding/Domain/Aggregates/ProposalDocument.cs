using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates;

public sealed class ProposalDocument
{
    public ProposalDocument(DocumentType documentType, S3Key s3Key, DateTimeOffset uploadedAt)
    {
        DocumentType = documentType;
        S3Key = s3Key;
        UploadedAt = uploadedAt;
    }

    public DocumentType DocumentType { get; }
    public S3Key S3Key { get; }
    public DateTimeOffset UploadedAt { get; }
}
