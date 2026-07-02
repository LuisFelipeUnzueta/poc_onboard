using Onboarding.Domain.ValueObjects;

namespace Onboarding.Application.Abstractions;

public interface IDocumentStorage
{
    Task UploadAsync(S3Key s3Key, Stream content, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(S3Key s3Key, CancellationToken cancellationToken);
}
