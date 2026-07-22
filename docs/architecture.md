# Architecture

## Overview

Merchant Onboarding Platform — a single-project .NET 10 solution structured with logical layers following Clean Architecture principles.

## Dependency Flow

```
Controllers ──► Application (via interfaces)
Application  ──► Domain
Domain       ──► (nothing)
Infrastructure ──► Application (implements ports)
```

## Layers

### Domain (`Onboarding.Domain.*`)

Pure business logic with zero external dependencies.

- **Aggregates**: `Proposal` (root), `Partner`, `ProposalDocument`
- **Value Objects**: `ProposalId`, `PartnerId`, `DocumentId`, `Cnpj`, `Cpf`, `Mcc`, `LegalName`, `S3Key`, `BankAccount`, `Address`
- **Enums**: `ProposalStatus`, `Segment`, `DocumentType`, `PersonType`, `BankAccountType`
- **Events**: `ProposalCreated`, `DocumentAttached`, `DocumentsCompleted`, `StatusChanged`
- **Common**: `Entity<T>`, `AggregateRoot<T>`, `Result<T>`, `DomainError`, `IDomainEvent`

### Application (`Onboarding.Application.*`)

Use cases and port interfaces. Depends only on Domain.

- **Use Cases**: `CreateProposalUseCase`, `GetProposalUseCase`, `UploadDocumentUseCase`
- **Ports**: `IProposalRepository`, `IDocumentStorage`, `IIdempotencyStore`, `IEventPublisher`, `IOutboxStore`, `IDocumentRulesService`, `IOnboardingMetrics`
- **Contracts**: Commands, Requests, Responses

### Infrastructure (`Onboarding.Infrastructure.*`)

Implements application ports against external services.

- **DynamoDb**: Single-table design with outbox pattern
- **Redis**: Idempotency store
- **S3**: Document storage (via LocalStack in dev)
- **Kafka**: Event publisher
- **Observability**: OpenTelemetry, Serilog, health checks, metrics

### Controllers (`Onboarding.Controllers`)

Minimal API surface — single controller with 3 endpoints.

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/proposals` | Create a new proposal |
| GET | `/proposals/{id}` | Get proposal details |
| POST | `/proposals/{id}/documents` | Upload a document |

### Workers (`Onboarding.Workers`)

Background services running in the same process.

- `OutboxPublisher`: Polls DynamoDB outbox table, publishes events to Kafka

## Object Mapping

Mapster is used for request-to-command mapping via `IRegister` implementations. Domain-to-DTO mapping uses explicit extension methods (`ProposalResponseMapper`) because Mapster's `RecordTypeAdapter` cannot resolve `.Value` properties on sealed record types with private constructors.

## Testing Strategy

| Project | Scope | Framework |
|---------|-------|-----------|
| `Onboarding.UnitTests` | Domain, Mappings, Middleware, Outbox | xUnit + FluentAssertions + NSubstitute |
| `Onboarding.ArchitectureTests` | Namespace dependency rules | xUnit + NetArchTest |
| `Onboarding.IntegrationTests` | End-to-end with Testcontainers | xUnit + Testcontainers |

## Infrastructure Services (docker-compose)

| Service | Image | Port |
|---------|-------|------|
| DynamoDB Local | amazon/dynamodb-local | 8000 |
| Redis | redis:7-alpine | 6379 |
| LocalStack (S3) | localstack/localstack:3 | 4566 |
| Kafka | bitnami/kafka:3.7 | 9092 |
