# Merchant Onboarding Platform

Backend POC para onboarding de merchants, implementado em .NET 10 e C# 14.

Estado atual: API core de proposals com persistencia em DynamoDB, idempotencia via Redis, upload de documentos em S3/LocalStack e documentacao com Scalar.

## Stack atual

- .NET 10
- C# 14
- ASP.NET Core Web API
- DynamoDB Local
- S3 via LocalStack
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
      S3/
tests/
  Onboarding.UnitTests/
docker-compose.yml
Onboarding.slnx
```

## Endpoints

- `GET /health/live`
- `POST /api/proposals`
- `GET /api/proposals/{proposalId}`
- `POST /api/proposals/{proposalId}/documents`
- `GET /scalar`
- `GET /openapi/v1.json`

### Upload de documento

```http
POST /api/proposals/{proposalId}/documents
Content-Type: multipart/form-data
```

Campos obrigatorios:

- `documentType`
- `file`

Tipos aceitos:

- `application/pdf`
- `image/jpeg`
- `image/png`

Limite: `10 MB`.

S3 key:

```text
proposals/{proposalId}/documents/{documentType}/{timestamp}-{sanitizedFilename}
```

Ao enviar todos os documentos obrigatorios, a proposta transiciona de `PendingDocuments` para `WaitingDocumentsApproval`.

## Como validar

```bash
dotnet restore
dotnet build --no-restore
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

Storage de documentos:

- Bucket S3 local: `onboarding-documents`
- Endpoint S3 local: `http://localhost:4566`
