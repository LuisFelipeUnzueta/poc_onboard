using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record ProposalId : UlidValue
{
    private ProposalId(string value)
        : base(value)
    {
    }

    public static ProposalId New() => new(NewUlid());

    public static Result<ProposalId> Create(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        var validation = Validate(normalized, nameof(ProposalId));

        return validation.IsSuccess
            ? Result<ProposalId>.Success(new ProposalId(normalized))
            : Result<ProposalId>.Failure(validation.Error!);
    }
}
