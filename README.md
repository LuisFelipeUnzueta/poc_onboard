# Merchant Onboarding Platform

Backend POC para onboarding de merchants, implementado em .NET 10 e C# 14.

Estado atual: API core de proposals com persistencia em DynamoDB, idempotencia via Redis e documentacao com Scalar.

## Stack atual

- .NET 10
- C# 14
- ASP.NET Core Web API
- DynamoDB Local
- Redis
- Scalar
- xUnit
- FluentAssertions
- NSubstitute
- Docker Compose

## Estrutura

```text
src/
  Onboarding/
    Controllers/
    Application/
    Domain/
      Aggregates/
      Common/
      Enums/
      Events/
      ValueObjects/
    Infrastructure/
      DynamoDb/
      Redis/
tests/
  Onboarding.UnitTests/
docker-compose.yml
Onboarding.slnx
```

## Endpoints

- `GET /health/live`
- `POST /api/proposals`
- `GET /api/proposals/{proposalId}`
- `GET /scalar`
- `GET /openapi/v1.json`

## Como validar

```bash
dotnet restore
dotnet build --no-restore
dotnet test tests/Onboarding.UnitTests --no-restore
```

## Como rodar

```bash
docker compose up -d
dotnet run --project src/Onboarding
```

Documentacao da API:

```text
http://localhost:5000/scalar
```

## Infra local

```bash
docker compose up -d
```

Servicos expostos:

- DynamoDB Local: `localhost:8000`
- Redis: `localhost:6379`
- LocalStack: `localhost:4566`
- Kafka: `localhost:9092`
