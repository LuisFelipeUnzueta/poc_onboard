# ADR-002: DynamoDB Single-Table Design

## Status

Accepted

## Context

We need a primary data store for proposals, documents, and outbox messages. Options include relational databases (PostgreSQL), NoSQL alternatives (MongoDB), or DynamoDB.

## Decision

Use **Amazon DynamoDB** with single-table design for all persistence.

## Consequences

**Positive:**
- Fully managed, serverless, auto-scaling
- Single-table design reduces operational overhead (one table to manage)
- Transactional writes support the outbox pattern (business data + outbox in one transaction)
- GSI1 enables partner-centric queries without denormalization
- Low latency at any scale

**Negative:**
- Query flexibility limited by access patterns (must be designed upfront)
- No ad-hoc queries or joins
- LocalStack for development introduces parity risk with real DynamoDB
- Steeper learning curve for single-table design
