using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record DocumentId : UlidValue
{
    private DocumentId(string value)
        : base(value)
    {
    }

    public static DocumentId New() => new(NewUlid());

    public static Result<DocumentId> Create(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        var validation = Validate(normalized, nameof(DocumentId));

        return validation.IsSuccess
            ? Result<DocumentId>.Success(new DocumentId(normalized))
            : Result<DocumentId>.Failure(validation.Error!);
    }
}
