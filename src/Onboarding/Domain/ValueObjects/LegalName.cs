using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record LegalName
{
    private LegalName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<LegalName> Create(string value)
    {
        var normalized = value.Trim();

        if (normalized.Length is < 1 or > 150)
        {
            return Result<LegalName>.Failure(DomainError.Validation("LegalName must have between 1 and 150 characters."));
        }

        return Result<LegalName>.Success(new LegalName(normalized));
    }
}
