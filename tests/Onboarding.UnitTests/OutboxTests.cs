using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Onboarding.Application.Abstractions;
using Onboarding.Domain.Aggregates;
using Onboarding.Domain.Enums;
using Onboarding.Domain.ValueObjects;
using Onboarding.Infrastructure.DynamoDb;
using Onboarding.Workers;

namespace Onboarding.UnitTests;

public sealed class OutboxTests
{
    [Fact]
    public async Task AddAsync_Should_Persist_Outbox_With_Aggregate()
    {
        var dynamoDb = Substitute.For<IAmazonDynamoDB>();
        TransactWriteItemsRequest? capturedRequest = null;
        dynamoDb.TransactWriteItemsAsync(
                Arg.Do<TransactWriteItemsRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new TransactWriteItemsResponse());
        var correlationContext = Substitute.For<ICorrelationContext>();
        correlationContext.CorrelationId.Returns("correlation-123");
        var repository = new DynamoDbProposalRepository(
            dynamoDb,
            Options.Create(new DynamoDbOptions()),
            correlationContext);
        var proposal = CreateProposal();

        await repository.AddAsync(proposal, CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.TransactItems.Should().HaveCount(3);
        var outboxItem = capturedRequest.TransactItems.Single(item =>
            item.Put?.Item.TryGetValue("Type", out var type) == true && type.S == "Outbox");
        outboxItem.Put.Item["EventType"].S.Should().Be("ProposalCreated");
        outboxItem.Put.Item["AggregateId"].S.Should().Be(proposal.Id.Value);
        outboxItem.Put.Item["CorrelationId"].S.Should().Be("correlation-123");
        proposal.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Publisher_Should_Publish_And_Mark_Message_As_Published()
    {
        var message = new OutboxMessage(
            "event-1", "ProposalCreated", "proposal-1", "Proposal", DateTimeOffset.UtcNow,
            "correlation-1", 1, "merchant.proposal.created", "{}");
        var outboxStore = Substitute.For<IOutboxStore>();
        outboxStore.GetPendingAsync(25, Arg.Any<CancellationToken>()).Returns([message]);
        var eventPublisher = Substitute.For<IEventPublisher>();
        var services = new ServiceCollection()
            .AddScoped<IOutboxStore>(_ => outboxStore)
            .AddScoped<IEventPublisher>(_ => eventPublisher)
            .BuildServiceProvider();
        var worker = new OutboxPublisher(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions()),
            NullLogger<OutboxPublisher>.Instance);

        await worker.PublishPendingAsync(CancellationToken.None);

        await eventPublisher.Received(1).PublishAsync(message, Arg.Any<CancellationToken>());
        await outboxStore.Received(1).MarkPublishedAsync(
            message.EventId,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publisher_Should_Not_Mark_Message_When_Kafka_Fails()
    {
        var message = new OutboxMessage(
            "event-1", "ProposalCreated", "proposal-1", "Proposal", DateTimeOffset.UtcNow,
            "correlation-1", 1, "merchant.proposal.created", "{}");
        var outboxStore = Substitute.For<IOutboxStore>();
        outboxStore.GetPendingAsync(25, Arg.Any<CancellationToken>()).Returns([message]);
        var eventPublisher = Substitute.For<IEventPublisher>();
        eventPublisher.PublishAsync(message, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Kafka unavailable")));
        var services = new ServiceCollection()
            .AddScoped<IOutboxStore>(_ => outboxStore)
            .AddScoped<IEventPublisher>(_ => eventPublisher)
            .BuildServiceProvider();
        var worker = new OutboxPublisher(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OutboxOptions()),
            NullLogger<OutboxPublisher>.Instance);

        await worker.PublishPendingAsync(CancellationToken.None);

        await outboxStore.DidNotReceive().MarkPublishedAsync(
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    private static Proposal CreateProposal()
    {
        var partner = Partner.Create("Joao Silva", Cpf.Create("529.982.247-25").Value!, 100, true).Value!;

        return Proposal.Create(
            PartnerId.New(),
            Cnpj.Create("11.444.777/0001-61").Value!,
            LegalName.Create("Empresa Exemplo Ltda").Value!,
            Segment.PayFac,
            Mcc.Create("5411").Value!,
            [partner],
            BankAccount.Create("60746948", "0001", "123456", "7", BankAccountType.CheckingAccount).Value!,
            Address.Create("01310-100", "Av. Paulista", "1000", null, "Bela Vista", "Sao Paulo", "SP").Value!).Value!;
    }
}
