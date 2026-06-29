using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record Address
{
    private static readonly HashSet<string> ValidStates =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG",
        "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    private Address(
        string zipCode,
        string street,
        string number,
        string? complement,
        string neighborhood,
        string city,
        string state)
    {
        ZipCode = zipCode;
        Street = street;
        Number = number;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        State = state;
    }

    public string ZipCode { get; }
    public string Street { get; }
    public string Number { get; }
    public string? Complement { get; }
    public string Neighborhood { get; }
    public string City { get; }
    public string State { get; }

    public static Result<Address> Create(
        string zipCode,
        string street,
        string number,
        string? complement,
        string neighborhood,
        string city,
        string state)
    {
        var normalizedZipCode = OnlyDigits(zipCode);
        var normalizedState = state.Trim().ToUpperInvariant();

        if (normalizedZipCode.Length != 8)
        {
            return Result<Address>.Failure(DomainError.Validation("ZipCode must have 8 digits."));
        }

        if (string.IsNullOrWhiteSpace(street)
            || string.IsNullOrWhiteSpace(number)
            || string.IsNullOrWhiteSpace(neighborhood)
            || string.IsNullOrWhiteSpace(city)
            || !ValidStates.Contains(normalizedState))
        {
            return Result<Address>.Failure(DomainError.Validation("Address must have street, number, neighborhood, city and valid state."));
        }

        return Result<Address>.Success(new Address(
            normalizedZipCode,
            street.Trim(),
            number.Trim(),
            string.IsNullOrWhiteSpace(complement) ? null : complement.Trim(),
            neighborhood.Trim(),
            city.Trim(),
            normalizedState));
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());
}
