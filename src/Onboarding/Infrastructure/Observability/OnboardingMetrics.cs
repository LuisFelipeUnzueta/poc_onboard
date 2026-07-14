using System.Diagnostics.Metrics;
using Onboarding.Application.Abstractions;
using Onboarding.Domain.Enums;

namespace Onboarding.Infrastructure.Observability;

public sealed class OnboardingMetrics : IOnboardingMetrics, IDisposable
{
    public const string MeterName = "Onboarding";
    private readonly Meter _meter = new(MeterName);

    public OnboardingMetrics()
    {
        ProposalsCreated = _meter.CreateCounter<long>("proposals_created_total");
        DocumentsUploaded = _meter.CreateCounter<long>("documents_uploaded_total");
        ProposalStatusTransitions = _meter.CreateCounter<long>("proposal_status_transitions_total");
        DocumentUploadDuration = _meter.CreateHistogram<double>("document_upload_duration_ms", "ms");
        IdempotencyCacheHits = _meter.CreateCounter<long>("idempotency_cache_hits_total");
    }

    public Counter<long> ProposalsCreated { get; }
    public Counter<long> DocumentsUploaded { get; }
    public Counter<long> ProposalStatusTransitions { get; }
    public Histogram<double> DocumentUploadDuration { get; }
    public Counter<long> IdempotencyCacheHits { get; }

    public void ProposalCreated(Segment segment) =>
        ProposalsCreated.Add(1, new KeyValuePair<string, object?>("segment", segment.ToString()));

    public void DocumentUploaded(DocumentType documentType, double durationMs)
    {
        DocumentsUploaded.Add(1,
            new KeyValuePair<string, object?>("document_type", documentType.ToString()),
            new KeyValuePair<string, object?>("status", "Received"));
        DocumentUploadDuration.Record(durationMs,
            new KeyValuePair<string, object?>("document_type", documentType.ToString()));
    }

    public void ProposalStatusTransitioned(ProposalStatus previousStatus, ProposalStatus newStatus) =>
        ProposalStatusTransitions.Add(1,
            new KeyValuePair<string, object?>("from_status", previousStatus.ToString()),
            new KeyValuePair<string, object?>("to_status", newStatus.ToString()));

    public void IdempotencyCacheHit(string endpoint) =>
        IdempotencyCacheHits.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint));

    public void Dispose() => _meter.Dispose();
}
