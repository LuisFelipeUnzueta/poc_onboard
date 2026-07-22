# Contributing

## Prerequisites

- .NET 10 SDK
- Docker Desktop

## Getting Started

1. Clone the repository
2. Start local infrastructure: `docker compose up -d`
3. Restore and build: `dotnet restore && dotnet build`
4. Run tests: `dotnet test`

## Development Workflow

1. Create a feature branch from `main`
2. Make changes following the architecture rules below
3. Run `dotnet build` and `dotnet test` before committing
4. Ensure architecture tests pass: `dotnet test tests/Onboarding.ArchitectureTests`
5. Submit a pull request

## Architecture Rules

The dependency flow must be respected at all times:

```
Controllers → Application (via interfaces)
Application → Domain
Domain → (nothing)
Infrastructure → implements Application ports
```

Enforced by architecture tests (`Onboarding.ArchitectureTests`). If you add a new layer or namespace, update the architecture tests accordingly.

## Code Style

- File-scoped namespaces (`namespace Foo;`)
- `var` when type is apparent
- Use `Result<T>` for error handling (no exceptions for business logic)
- Use `DomainError` static members for well-known errors
- Prefix use case classes with the operation name and suffix with `UseCase`
- Value objects must validate in their `Create` factory method
- Domain events must implement `IDomainEvent`

## Testing

- **Unit tests**: Test domain logic, mappings, and isolated use case behavior
- **Architecture tests**: Verify namespace dependencies
- **Integration tests**: End-to-end with Testcontainers (DynamoDB, Redis, Kafka)

Minimum coverage targets:

| Layer | Coverage |
|-------|----------|
| Domain | 90% |
| Application | 80% |
| Infrastructure | 60% |
| Overall | 75% |

## Commit Messages

Use clear, concise commit messages. Prefer:

- `Add <feature>` for new functionality
- `Fix <issue>` for bug fixes
- `Refactor <area>` for code improvements
- `Update <area>` for documentation or config changes
