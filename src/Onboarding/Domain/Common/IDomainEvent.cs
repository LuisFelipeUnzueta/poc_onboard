namespace Onboarding.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
