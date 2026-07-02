using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Onboarding.Application.Abstractions;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Infrastructure.S3;

public sealed class S3DocumentStorage(
    IAmazonS3 s3,
    IOptions<S3Options> options) : IDocumentStorage
{
    private readonly string _bucketName = options.Value.BucketName;

    public async Task UploadAsync(S3Key s3Key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key.Value,
            InputStream = content,
            ContentType = contentType
        }, cancellationToken);
    }

    public async Task DeleteAsync(S3Key s3Key, CancellationToken cancellationToken)
    {
        await s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = s3Key.Value
        }, cancellationToken);
    }
}
