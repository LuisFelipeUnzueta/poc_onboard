# DynamoDB Modeling

## Table Design

Single-table design using `OnboardingTable` with composite keys.

### Entity Types

| Entity | PK | SK | GSI1-PK | GSI1-SK |
|--------|----|----|---------|---------|
| Proposal | `PROPOSAL#<proposalId>` | `METADATA` | `PARTNER#<partnerId>` | `PROPOSAL#<proposalId>` |
| Proposal Document | `PROPOSAL#<proposalId>` | `DOC#<documentType>` | — | — |
| Outbox Message | `OUTBOX#<messageId>` | `OUTBOX` | `OUTBOX#<status>` | `#<createdAt>` |

### Attributes

#### Proposal

```
PK:             PROPOSAL#<proposalId>
SK:             METADATA
GSI1PK:         PARTNER#<partnerId>
GSI1SK:         PROPOSAL#<proposalId>
PartnerId:      <partnerId>
Cnpj:           <cnpj>
LegalName:      <legalName>
Segment:        <segment>
Mcc:            <mcc>
Status:         <proposalStatus>
BankAccount:    { Ispb, Agency, AccountNumber, AccountDigit, BankAccountType }
Address:        { ZipCode, Street, Number, Complement, Neighborhood, City, State }
Partners:       [ { Name, Cpf, ParticipationPercentage, IsLegalRepresentative } ]
CreatedAt:      <dateTime>
UpdatedAt:      <dateTime>
Version:        <int>
EntityType:     Proposal
```

#### Proposal Document

```
PK:             PROPOSAL#<proposalId>
SK:             DOC#<documentType>
DocumentId:     <documentId>
DocumentType:   <documentType>
S3Key:          <s3Key>
UploadedAt:     <dateTime>
EntityType:     Document
```

#### Outbox Message

```
PK:             OUTBOX#<messageId>
SK:             OUTBOX
GSI1PK:         OUTBOX#<status>
GSI1SK:         #<createdAt>
EventType:      <eventType>
AggregateId:    <proposalId>
Payload:        <json>
Status:         Pending | Published | Failed
CreatedAt:      <dateTime>
PublishedAt:    <dateTime?>
EntityType:     Outbox
```

## Access Patterns

1. **Get proposal by ID**: `PK = PROPOSAL#<id>, SK = METADATA`
2. **Get all documents for proposal**: `PK = PROPOSAL#<id>, SK begins_with DOC#`
3. **Get proposals by partner**: `GSI1: PK = PARTNER#<partnerId>`
4. **Get pending outbox messages**: `GSI1: PK = OUTBOX#Pending, SK begins_with #`
5. **Get all proposal data (single query)**: `PK = PROPOSAL#<id>` (returns metadata + documents)

## Concurrency

- Optimistic concurrency using `Version` attribute with `ConditionExpression` on `attribute_version = :expected`
- Outbox writes happen in the same DynamoDB transaction as business data (transactional write)

## TTL

No TTL configured. Data is retained permanently for audit purposes.
