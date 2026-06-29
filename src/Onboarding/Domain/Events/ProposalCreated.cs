using Onboarding.Domain.Common;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Events;

public sealed record ProposalCreated(ProposalId ProposalId, DateTimeOffset OccurredAt) : IDomainEvent;
