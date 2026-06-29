using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;

namespace Onboarding.Domain.ValueObjects;

public sealed record BankAccount
{
    private BankAccount(
        string ispb,
        string agency,
        string accountNumber,
        string accountDigit,
        BankAccountType accountType)
    {
        Ispb = ispb;
        Agency = agency;
        AccountNumber = accountNumber;
        AccountDigit = accountDigit;
        AccountType = accountType;
    }

    public string Ispb { get; }
    public string Agency { get; }
    public string AccountNumber { get; }
    public string AccountDigit { get; }
    public BankAccountType AccountType { get; }

    public static Result<BankAccount> Create(
        string ispb,
        string agency,
        string accountNumber,
        string accountDigit,
        BankAccountType accountType)
    {
        var normalizedIspb = OnlyDigits(ispb);
        var normalizedAgency = OnlyDigits(agency);
        var normalizedAccountNumber = OnlyDigits(accountNumber);
        var normalizedAccountDigit = accountDigit.Trim().ToUpperInvariant();

        if (normalizedIspb.Length != 8)
        {
            return Result<BankAccount>.Failure(DomainError.Validation("ISPB must have 8 digits."));
        }

        if (normalizedAgency.Length is < 1 or > 6)
        {
            return Result<BankAccount>.Failure(DomainError.Validation("Agency must have between 1 and 6 digits."));
        }

        if (normalizedAccountNumber.Length is < 1 or > 20)
        {
            return Result<BankAccount>.Failure(DomainError.Validation("Account number must have between 1 and 20 digits."));
        }

        if (normalizedAccountDigit.Length != 1 || !char.IsLetterOrDigit(normalizedAccountDigit[0]))
        {
            return Result<BankAccount>.Failure(DomainError.Validation("Account digit must have 1 alphanumeric character."));
        }

        return Result<BankAccount>.Success(new BankAccount(
            normalizedIspb,
            normalizedAgency,
            normalizedAccountNumber,
            normalizedAccountDigit,
            accountType));
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());
}
