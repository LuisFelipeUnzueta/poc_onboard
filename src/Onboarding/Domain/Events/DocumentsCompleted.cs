using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Events;

public sealed record DocumentsCompleted(
    ProposalId ProposalId,
    DateTimeOffset OccurredAt) : IDomainEvent;
