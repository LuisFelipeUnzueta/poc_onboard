using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record Cnpj
{
    private Cnpj(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Cnpj> Create(string value)
    {
        var digits = OnlyDigits(value);

        if (digits.Length != 14 || digits.Distinct().Count() == 1 || !HasValidCheckDigits(digits))
        {
            return Result<Cnpj>.Failure(DomainError.Validation("CNPJ must be valid."));
        }

        return Result<Cnpj>.Success(new Cnpj(digits));
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());

    private static bool HasValidCheckDigits(string digits)
    {
        int[] firstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] secondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

        return CalculateDigit(digits, firstWeights) == digits[12] - '0'
            && CalculateDigit(digits, secondWeights) == digits[13] - '0';
    }

    private static int CalculateDigit(string digits, int[] weights)
    {
        var sum = weights.Select((weight, index) => (digits[index] - '0') * weight).Sum();
        var remainder = sum % 11;

        return remainder < 2 ? 0 : 11 - remainder;
    }
}
