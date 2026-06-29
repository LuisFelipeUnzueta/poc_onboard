using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Aggregates;

public sealed class Partner
{
    private Partner(string name, Cpf cpf, decimal participationPercentage, bool isLegalRepresentative)
    {
        Name = name;
        Cpf = cpf;
        ParticipationPercentage = participationPercentage;
        IsLegalRepresentative = isLegalRepresentative;
    }

    public string Name { get; }
    public Cpf Cpf { get; }
    public decimal ParticipationPercentage { get; }
    public bool IsLegalRepresentative { get; }

    public static Result<Partner> Create(
        string name,
        Cpf cpf,
        decimal participationPercentage,
        bool isLegalRepresentative)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Partner>.Failure(DomainError.Validation("Partner name is required."));
        }

        if (participationPercentage <= 0 || participationPercentage > 100)
        {
            return Result<Partner>.Failure(DomainError.Validation("Partner participation must be between 0 and 100."));
        }

        return Result<Partner>.Success(new Partner(name.Trim(), cpf, participationPercentage, isLegalRepresentative));
    }
}
