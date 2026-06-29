using System.Text.RegularExpressions;
using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;

namespace Onboarding.Domain.ValueObjects;

public sealed partial record S3Key
{
    private S3Key(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<S3Key> Create(string value)
    {
        var normalized = value.Trim();

        if (!KeyPattern().IsMatch(normalized) || !ContainsValidDocumentType(normalized))
        {
            return Result<S3Key>.Failure(DomainError.Validation("S3Key must match proposals/{proposalId}/documents/{type}/{filename}."));
        }

        return Result<S3Key>.Success(new S3Key(normalized));
    }

    private static bool ContainsValidDocumentType(string value)
    {
        var documentType = value.Split('/')[3];

        return Enum.TryParse<DocumentType>(documentType, ignoreCase: false, out _);
    }

    [GeneratedRegex(@"^proposals/[0-9A-HJKMNP-TV-Z]{26}/documents/[A-Za-z]+/.+$")]
    private static partial Regex KeyPattern();
}
