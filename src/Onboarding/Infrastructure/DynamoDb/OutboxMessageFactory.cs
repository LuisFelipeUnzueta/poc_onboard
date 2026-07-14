using System.Diagnostics;
using System.Text.Json;
using Onboarding.Application.Abstractions;
using Onboarding.Domain.Common;
using Onboarding.Domain.Enums;
using Onboarding.Domain.Events;

namespace Onboarding.Infrastructure.DynamoDb;

internal static class OutboxMessageFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<OutboxMessage> Create(IEnumerable<IDomainEvent> domainEvents, int version, string? correlationId)
    {
        var effectiveCorrelationId = correlationId
            ?? Activity.Current?.TraceId.ToString()
            ?? Guid.CreateVersion7().ToString();

        return domainEvents
            .Select(domainEvent => Map(domainEvent, version, effectiveCorrelationId))
            .Where(message => message is not null)
            .Cast<OutboxMessage>()
            .ToArray();
    }

    private static OutboxMessage? Map(IDomainEvent domainEvent, int version, string correlationId)
    {
        var mapped = domainEvent switch
        {
            ProposalCreated @event => new ExternalEvent("ProposalCreated", @event.ProposalId.Value, "merchant.proposal.created", new { proposalId = @event.ProposalId.Value }),
            DocumentAttached @event => new ExternalEvent("DocumentUploaded", @event.ProposalId.Value, "merchant.document.uploaded", new
            {
                proposalId = @event.ProposalId.Value,
                documentId = @event.DocumentId.Value,
                documentType = @event.DocumentType.ToString(),
                s3Key = @event.S3Key.Value
            }),
            DocumentsCompleted @event => new ExternalEvent("DocumentsCompleted", @event.ProposalId.Value, "merchant.documents.completed", new { proposalId = @event.ProposalId.Value }),
            StatusChanged { NewStatus: ProposalStatus.Approved } @event => new ExternalEvent("ProposalApproved", @event.ProposalId.Value, "merchant.proposal.approved", StatusPayload(@event)),
            StatusChanged { NewStatus: ProposalStatus.SubmittedToAcquirer } @event => new ExternalEvent("ProposalSubmittedToAcquirer", @event.ProposalId.Value, "merchant.proposal.submitted", StatusPayload(@event)),
            _ => null
        };

        return mapped is null
            ? null
            : new OutboxMessage(
                Guid.CreateVersion7().ToString(), mapped.EventType, mapped.AggregateId, "Proposal",
                domainEvent.OccurredAt, correlationId, version, mapped.Topic,
                JsonSerializer.Serialize(mapped.Payload, JsonOptions));
    }

    private static object StatusPayload(StatusChanged @event) => new
    {
        proposalId = @event.ProposalId.Value,
        previousStatus = @event.PreviousStatus.ToString(),
        newStatus = @event.NewStatus.ToString()
    };

    private sealed record ExternalEvent(string EventType, string AggregateId, string Topic, object Payload);
}
