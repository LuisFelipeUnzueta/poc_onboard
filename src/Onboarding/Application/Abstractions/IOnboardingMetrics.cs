using Onboarding.Domain.Enums;

namespace Onboarding.Application.Abstractions;

public interface IOnboardingMetrics
{
    void ProposalCreated(Segment segment);
    void DocumentUploaded(DocumentType documentType, double durationMs);
    void ProposalStatusTransitioned(ProposalStatus previousStatus, ProposalStatus newStatus);
    void IdempotencyCacheHit(string endpoint);
}
