using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Events;

public sealed record StatusChanged(
    ProposalId ProposalId,
    ProposalStatus PreviousStatus,
    ProposalStatus NewStatus,
    DateTimeOffset OccurredAt) : IDomainEvent;
