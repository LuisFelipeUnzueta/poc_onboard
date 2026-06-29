using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record Mcc
{
    private static readonly HashSet<string> ValidMccs = ["5411", "5812", "5814", "5999", "6012", "7399"];

    private Mcc(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Mcc> Create(string value)
    {
        var normalized = value.Trim();

        if (normalized.Length != 4 || !normalized.All(char.IsDigit) || !ValidMccs.Contains(normalized))
        {
            return Result<Mcc>.Failure(DomainError.Validation("MCC must be valid."));
        }

        return Result<Mcc>.Success(new Mcc(normalized));
    }
}
