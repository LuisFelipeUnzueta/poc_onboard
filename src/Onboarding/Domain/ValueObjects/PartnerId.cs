using Onboarding.Domain.Common;

namespace Onboarding.Domain.ValueObjects;

public sealed record PartnerId : UlidValue
{
    private PartnerId(string value)
        : base(value)
    {
    }

    public static PartnerId New() => new(NewUlid());

    public static Result<PartnerId> Create(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        var validation = Validate(normalized, nameof(PartnerId));

        return validation.IsSuccess
            ? Result<PartnerId>.Success(new PartnerId(normalized))
            : Result<PartnerId>.Failure(validation.Error!);
    }
}
