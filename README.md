# Merchant Onboarding Platform

Backend platform for merchant onboarding, implemented in .NET 10 and C# 14.

Proposal lifecycle: creation → document upload → pricing → risk analysis → approval/rejection → acquirer submission.

## Stack

- .NET 10 / C# 14
- ASP.NET Core Web API
- DynamoDB (single-table design, outbox pattern)
- S3 via LocalStack
- Redis (idempotency)
- Kafka (event publishing)
- Mapster (object mapping)
- OpenTelemetry + Serilog (observability)
- xUnit + FluentAssertions + NSubstitute + NetArchTest
- Docker Compose (local infrastructure)

## Architecture

Single-project with logical layers following Clean Architecture dependency flow:

```
Controllers → Application (interfaces) → Domain ← Infrastructure
```

See [docs/architecture.md](docs/architecture.md) for details.

## Structure

```text
src/
  Onboarding/
    Controllers/           # API endpoints
    Application/           # Use cases, ports, contracts
    Domain/                # Entities, value objects, events
    Infrastructure/        # DynamoDB, Redis, S3, Kafka adapters
    Mappings/              # Mapster IRegister + ProposalResponseMapper
    Extensions/            # DI registration
    Middleware/            # Correlation ID
    Workers/               # Outbox publisher
tests/
  Onboarding.UnitTests/
  Onboarding.ArchitectureTests/
  Onboarding.IntegrationTests/
docs/
  architecture.md
  dynamodb-modeling.md
  api-examples.http
  decisions/              # ADR-001 through ADR-006
```

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/health/live` | Liveness check |
| GET | `/health/ready` | Readiness check (DynamoDB, Redis, Kafka) |
| POST | `/proposals` | Create proposal |
| GET | `/proposals/{id}` | Get proposal details |
| POST | `/proposals/{id}/documents` | Upload document |
| GET | `/scalar` | API documentation UI |

## How to Run

### Prerequisites

- .NET 10 SDK
- Docker Desktop

### Start infrastructure

```bash
docker compose up -d
```

Services:

| Service | Endpoint |
|---------|----------|
| DynamoDB Local | `localhost:8000` |
| Redis | `localhost:6379` |
| LocalStack (S3) | `localhost:4566` |
| Kafka | `localhost:9092` |

### Run the API

```bash
dotnet run --project src/Onboarding
```

API docs: http://localhost:5000/scalar

### Run Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test tests/Onboarding.UnitTests

# Architecture tests only
dotnet test tests/Onboarding.ArchitectureTests
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).
