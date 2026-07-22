# ADR-001: Single-Project with Logical Layers

## Status

Accepted

## Context

We need to decide the project structure for the Merchant Onboarding Platform. Options include separate projects per layer (classic Clean Architecture) or a single project with logical layer separation via namespaces.

## Decision

Use a **single ASP.NET Core project** (`Onboarding`) with logical layers organized by namespace.

## Consequences

**Positive:**
- Simpler deployment and CI/CD pipeline (one build artifact)
- Faster build times
- Reduced ceremony for a proof-of-concept / early-stage platform
- Internal types accessible across layers without `InternalsVisibleTo` between projects

**Negative:**
- Compiler does not enforce layer boundaries (replaced by architecture tests via NetArchTest)
- Potential for accidental coupling if discipline lapses
- IDE navigation is slightly harder without project-level separation
