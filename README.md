# Merchant Onboarding Platform

Backend POC para onboarding de merchants, implementado em .NET 10 e C# 14.

Estado atual: fundacao do projeto com um unico projeto produtivo e separacao por pastas internas.

## Stack atual

- .NET 10
- C# 14
- ASP.NET Core Web API
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
tests/
  Onboarding.UnitTests/
docker-compose.yml
Onboarding.slnx
```

## Como validar

```bash
dotnet restore
dotnet build --no-restore
dotnet test tests/Onboarding.UnitTests --no-restore
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