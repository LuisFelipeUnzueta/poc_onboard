# ADR-005: xUnit + FluentAssertions + NSubstitute

## Status

Accepted

## Context

We need a testing stack for unit, architecture, and integration tests. Options include NUnit, xUnit, MSTest for frameworks; Moq, NSubstitute, FakeItEasy for mocking; FluentAssertions, Shouldly, xUnit assertions for assertions.

## Decision

Use **xUnit** as the test framework, **FluentAssertions** for readable assertions, and **NSubstitute** for mocking.

## Consequences

**Positive:**
- xUnit is the de facto standard for .NET with strong community support
- FluentAssertions provides expressive, self-documenting test assertions
- NSubstitute has a clean, minimal API that reduces test boilerplate
- Architecture tests use **NetArchTest** for namespace dependency verification

**Negative:**
- Three assertion/mocking libraries to learn (minor)
- FluentAssertions v7 has breaking changes from v6 (migration effort already paid)
