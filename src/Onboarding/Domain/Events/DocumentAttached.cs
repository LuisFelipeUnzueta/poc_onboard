using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;

namespace Onboarding.Domain.Events;

public sealed record DocumentAttached(
    ProposalId ProposalId,
    DocumentType DocumentType,
    S3Key S3Key,
    DateTimeOffset OccurredAt) : IDomainEvent;
