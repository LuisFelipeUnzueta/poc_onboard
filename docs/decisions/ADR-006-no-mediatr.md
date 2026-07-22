# ADR-006: No MediatR — Direct Use Case Invocation

## Status

Accepted

## Context

Many Clean Architecture templates use MediatR as a mediator between controllers and use cases. We need to decide whether to introduce a mediator or call use cases directly.

## Decision

**Do not use MediatR.** Controllers inject use case interfaces (`ICreateProposalUseCase`, `IGetProposalUseCase`, `IUploadDocumentUseCase`) via DI and call them directly.

## Consequences

**Positive:**
- Simpler debugging (no mediator pipeline to step through)
- Clear, explicit dependency graph visible in constructor injection
- Reduced package footprint and build time
- Easier to reason about request flow
- No mediator pipeline ordering concerns

**Negative:**
- No built-in cross-cutting concern pipeline (handled by middleware instead)
- Each use case requires explicit DI registration (no auto-discovery)
- If cross-cutting concerns multiply, manual pipeline management becomes heavier
