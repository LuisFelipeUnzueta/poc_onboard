using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record Cpf
{
    private Cpf(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<Cpf> Create(string value)
    {
        var digits = OnlyDigits(value);

        if (digits.Length != 11 || digits.Distinct().Count() == 1 || !HasValidCheckDigits(digits))
        {
            return Result<Cpf>.Failure(DomainError.Validation("CPF must be valid."));
        }

        return Result<Cpf>.Success(new Cpf(digits));
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());

    private static bool HasValidCheckDigits(string digits)
    {
        return CalculateDigit(digits, 9, 10) == digits[9] - '0'
            && CalculateDigit(digits, 10, 11) == digits[10] - '0';
    }

    private static int CalculateDigit(string digits, int length, int initialWeight)
    {
        var sum = Enumerable.Range(0, length).Sum(index => (digits[index] - '0') * (initialWeight - index));
        var remainder = sum % 11;

        return remainder < 2 ? 0 : 11 - remainder;
    }
}
