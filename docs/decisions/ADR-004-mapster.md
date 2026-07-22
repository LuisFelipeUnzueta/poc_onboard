# ADR-004: Mapster for Object Mapping

## Status

Accepted

## Context

We need object mapping between layers (Request→Command, Domain→Response). Options include AutoMapper, Mapster, and manual mapping.

## Decision

Use **Mapster** (via local fork `Net_Mapper`) with `IRegister` interface for request-to-command mapping. Use explicit extension methods for domain-to-DTO mapping where Mapster's `RecordTypeAdapter` cannot handle sealed record value objects.

## Consequences

**Positive:**
- `IRegister` provides type-safe, discoverable mapping configuration
- `TypeAdapterConfig.GlobalSettings.Scan()` + `Compile()` catches mapping errors at startup
- Local fork allows customization and bug fixes without waiting for upstream releases

**Negative:**
- Mapster's expression tree compiler cannot access `.Value` on sealed records with private constructors — requires manual `ProposalResponseMapper` for domain-to-DTO mappings
- Local fork adds maintenance burden
