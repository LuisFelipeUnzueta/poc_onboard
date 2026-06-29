using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record RazaoSocial
{
    private RazaoSocial(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<RazaoSocial> Create(string value)
    {
        var normalized = value.Trim();

        if (normalized.Length is < 1 or > 150)
        {
            return Result<RazaoSocial>.Failure(DomainError.Validation("RazaoSocial must have between 1 and 150 characters."));
        }

        return Result<RazaoSocial>.Success(new RazaoSocial(normalized));
    }
}
