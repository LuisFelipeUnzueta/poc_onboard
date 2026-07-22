# ADR-003: Outbox Pattern for Event Publishing

## Status

Accepted

## Context

We need reliable event publishing to Kafka when proposal state changes. The naive approach (publish directly) risks data loss if the publisher fails after the database write.

## Decision

Implement the **Transactional Outbox Pattern**: write domain events to an outbox table in the same DynamoDB transaction as business data, then publish via a background worker polling the outbox.

## Consequences

**Positive:**
- Guaranteed at-least-once delivery (dual-write problem solved)
- Consistent state between business data and events
- Failed publishes are retried automatically
- Decouples publish timing from request handling

**Negative:**
- Eventual consistency for downstream consumers (events arrive with delay)
- Outbox polling adds latency (configurable poll interval)
- Duplicate delivery possible — consumers must be idempotent
