---
name: anki67-dotnet-engineering
description: Apply concise, SOLID, performance-conscious C# and .NET engineering to Anki67 architecture, implementation, refactoring, review, and tests. Use for .NET projects, ASP.NET Core, Blazor, application features, and integrations; skip purely visual CSS or standalone JavaScript work.
---

# Anki67 .NET Engineering

Produce maintainable, testable production code using the latest stable C# supported by the repository's target framework. Be concise and explicit. Never trade readability, correctness, compatibility, or measured performance for novelty.

## Required approach

1. Inspect the target framework, project references, nearby conventions, tests, and current working-tree changes before editing.
2. Place every production responsibility in the correct layer before implementing it. Do not add business or integration logic to API endpoints, Blazor components, or AppHost.
3. Use the latest stable compiler features supported by the target framework. Do not enable preview features or use `LangVersion=latest` unless the user explicitly requests non-reproducible preview adoption.
4. Apply SOLID to real change boundaries; do not create interfaces, patterns, or indirection without a responsibility or dependency seam.
5. Unit-test deterministic Domain and Application behavior whenever possible. Every bug fix gets a regression test. Skip tests only for trivial wiring/generated code or when the behavior cannot be isolated; state the reason.
6. Design the simplest correct API and data flow, then optimize meaningful costs. Do not introduce pooling, spans, custom caching, or concurrency without a concrete workload benefit.
7. Apply the Boy Scout rule within the touched area: leave names, nullability, warnings, tests, and structure cleaner than found. Keep cleanup bounded; report architectural debt that would materially expand the request.
8. Build and run relevant tests plus the solution build. New warnings, broken dependency direction, or unverified behavior mean the work is incomplete.

## Layering invariant

Anki67 production code uses separate `Anki67.Domain`, `Anki67.Application`, and `Anki67.Infrastructure` projects. Existing host projects remain thin outer layers:

- `Anki67.Domain`: business concepts and invariants; no infrastructure or framework dependencies.
- `Anki67.Application`: use cases, orchestration, ports, commands/queries, and application contracts.
- `Anki67.Infrastructure`: AnkiConnect, KanjiVG, persistence, HTTP providers, files, clocks, and other port implementations.
- `Anki67.Api`: transport, authentication, HTTP mapping, error mapping, and dependency-injection composition only.
- `Anki67.Web`: presentation state and API calls only; no direct infrastructure access or duplicated business rules.
- `Anki67.AppHost`: local orchestration only.

Dependencies point inward: Domain has no project dependencies; Application references Domain; Infrastructure references Application and Domain; API references Application and Infrastructure. Never reference Infrastructure from Domain or Application.

When a required layer is missing, create or use the correct project before adding a production feature. Migrate directly related legacy code while touching it; do not perform unrelated repository-wide rewrites without authorization.

Read [references/architecture.md](references/architecture.md) whenever adding a project, feature boundary, integration, domain model, use case, endpoint, or dependency.

## SOLID invariant

- **S:** one cohesive responsibility and one reason to change per type or module.
- **O:** add providers or policies behind stable ports; avoid growing central type/action conditionals.
- **L:** every implementation preserves its contract, including nullability, cancellation, errors, ordering, and side effects.
- **I:** expose small consumer-focused interfaces; never use broad service or repository interfaces as dependency bags.
- **D:** Domain and Application depend on abstractions they own; Infrastructure implements them; API composes them.

Prefer composition over inheritance. Add abstractions at genuine seams, not pre-emptively. Protect dependency direction with project references and architecture tests.

## C# and .NET style

Use file-scoped namespaces, nullable reference types, implicit usings, collection expressions, pattern matching, switch expressions, records, primary constructors, `required`/`init`, raw string literals, and C# 14 features when each one improves the local design. Prefer immutable messages and value objects. Avoid clever expression compression, positional records with unclear arguments, needless abstractions, and speculative generic frameworks.

Read [references/csharp-style.md](references/csharp-style.md) for any C#, Razor, public API, DTO, exception, logging, or asynchronous-code change.

## Performance and quality

Prefer sound algorithms, bounded I/O, streaming where useful, cancellation, reuse of `HttpClient`, and avoiding unnecessary materialization. Keep hot paths allocation-aware, but measure before applying low-level optimizations. Use source generation where it materially improves startup, trimming, throughput, or logging overhead.

Read [references/performance-quality.md](references/performance-quality.md) when implementing I/O, serialization, concurrency, caching, media processing, external providers, loops over potentially large inputs, or tests.

## Completion check

- Correct layer and dependency direction.
- Public and cross-layer contracts are explicit and minimal.
- SOLID responsibilities and substitutions hold without speculative abstraction.
- Async I/O accepts and propagates `CancellationToken`.
- Errors are actionable and sensitive data is not logged.
- Deterministic Domain/Application behavior has unit coverage; bug fixes have regression coverage.
- Integration boundaries have proportionate contract or functional coverage.
- Formatting, relevant tests, and the solution build pass without new warnings.
- The final report identifies verification performed and any bounded cleanup intentionally deferred.
