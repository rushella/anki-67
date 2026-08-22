# Anki67 layered architecture

Use this reference for project placement, dependencies, and feature boundaries.

## Projects and responsibilities

### Anki67.Domain

Own stable business meaning:

- aggregates, entities, value objects, domain events, and domain services;
- invariants and state transitions;
- domain-specific errors or result types that do not depend on transports;
- pure policies that can execute without network, filesystem, clock, database, ASP.NET Core, or Blazor.

Do not reference Microsoft.Extensions, ASP.NET Core, EF Core, HTTP clients, JSON serializers, or infrastructure DTOs. Prefer behavior-rich domain types over public setters and anemic bags of data.

### Anki67.Application

Own application behavior:

- commands, queries, handlers, workflows, and transaction boundaries;
- interfaces for external capabilities such as Anki, media, pitch, audio, storage, and time;
- input/output models used by use cases;
- validation that depends on a use case rather than a single domain object;
- authorization decisions expressed independently of HTTP.

Application references Domain. It defines ports; Infrastructure implements them. Keep handlers focused on one use case and avoid hiding simple flows behind unnecessary mediator or repository abstractions.

### Anki67.Infrastructure

Own replaceable technical details:

- AnkiConnect and external HTTP clients;
- KanjiVG, audio, pitch, image, and AI providers;
- persistence, caches, filesystem, serialization details, and system adapters;
- options binding and service-registration extensions for these implementations.

Infrastructure references Application and Domain. Convert provider payloads at the boundary; do not leak vendor DTOs into Application or Domain.

### Hosts and presentation

`Anki67.Api` is the composition root. Endpoints parse transport input, invoke one Application use case, and map its output to HTTP. Keep endpoints free of business rules and provider calls.

`Anki67.Web` owns responsive UI, view state, and typed API clients. It must not call AnkiConnect or other infrastructure directly. Shared wire contracts may live in a deliberately small contracts project if sharing prevents duplication without coupling Web to Application internals.

`Anki67.AppHost` declares processes, references, endpoints, and development orchestration. It contains no application behavior.

## Dependency graph

```text
Anki67.Domain
      ▲
      │
Anki67.Application
      ▲
      │
Anki67.Infrastructure
      ▲
      │
Anki67.Api (composition root)

Anki67.Web ──HTTP──► Anki67.Api
Anki67.AppHost ──orchestrates──► Web + API
```

Infrastructure may depend inward; inner layers never depend outward. API may reference Infrastructure solely to compose implementations.

## Feature slices inside layers

Group code by cohesive feature within each layer when that makes navigation easier, for example `CardMining`, `StrokeOrder`, or `AnkiGateway`. Avoid generic dumping grounds named `Helpers`, `Managers`, or `Services`.

Keep each cross-layer flow traceable:

```text
API request
  → Application command/query
  → Domain behavior
  → Application port
  → Infrastructure adapter
```

## Boundary rules

- Domain never consumes transport or provider DTOs.
- Application never constructs `HttpClient`, reads environment variables, or accesses files directly.
- Infrastructure does not decide business policy.
- API does not catch every exception ad hoc; use centralized, consistent problem-details mapping.
- Web never relies on server implementation types.
- Configuration and secrets enter through options at the outer boundary.
- Add interfaces only at actual dependency boundaries or where multiple behaviorally meaningful implementations exist.

## SOLID review rules

Apply SOLID as five observable checks:

- **Single Responsibility:** a type has one cohesive purpose. Split mixed orchestration, transport mapping, persistence, rendering, or provider access at their natural boundaries.
- **Open/Closed:** adding a card step or external provider should normally add an implementation and registration, not edit a growing central conditional. Prefer a direct switch when the alternatives are small, closed, and domain-defined.
- **Liskov Substitution:** implementations honor the full port contract: accepted inputs, returned outcomes, nullability, cancellation, ordering, idempotency, side effects, and exception semantics. Run shared contract tests when a port has multiple implementations.
- **Interface Segregation:** define narrow interfaces around consumer use cases. Split read/write or unrelated capabilities; do not create `IEverythingService`, generic repositories, or marker interfaces.
- **Dependency Inversion:** Application owns external-capability ports. Infrastructure implements them. API selects implementations. Inner layers never resolve services or import outer-layer types.

Prefer composition and explicit delegation over inheritance. Abstract behavior only when a seam exists now: an external dependency, meaningful substitution, independent testing boundary, or stable policy variation.

## Test placement

- `Anki67.Domain.Tests`: invariants, value semantics, domain policies, and state transitions.
- `Anki67.Application.Tests`: use cases with focused fakes for ports, including cancellation and expected failure paths.
- `Anki67.Infrastructure.Tests`: provider contract, serialization, persistence, and integration behavior.
- `Anki67.Api.Tests`: functional transport, validation, authentication/CORS, and error mapping.
- Web tests: meaningful component behavior and typed client contracts; do not repeat Application tests.

Add an architecture test when project references or namespace dependencies could regress silently.

## Boy Scout migration

When touched legacy code is in the wrong host project, move the directly related responsibility into the correct layer as part of the feature. Preserve public behavior and add tests around the seam. If the move would cascade across unrelated features, keep the requested change safe, document the debt, and propose a separate migration rather than churning the repository.
