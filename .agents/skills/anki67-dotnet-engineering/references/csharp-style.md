# Modern C# and .NET style

Use this reference for implementation and review of C#, Razor, APIs, and contracts.

## Language baseline

- Use the latest stable C# version implied by the repository target framework and installed supported SDK. For `net10.0`, use stable C# 14.
- Keep builds reproducible. Do not select preview or floating language versions unless explicitly requested.
- Preserve nullable reference types and resolve warnings rather than suppressing them broadly.
- Follow the repository `.editorconfig`; add or adjust rules centrally when a convention should apply everywhere.

## Concise features

Prefer when they reduce ceremony without hiding meaning:

- file-scoped namespaces and implicit usings;
- collection expressions such as `[]` and spread elements where the target type is clear;
- primary constructors for small dependency-injected or immutable types when captured state remains obvious;
- records for immutable messages and value semantics, and `readonly record struct` for small value objects when copying cost is acceptable;
- property, list, relational, and type patterns with switch expressions for closed decision tables;
- `required` and `init` for construction-time invariants that do not belong in a richer domain constructor;
- raw string literals for JSON, HTML, SQL, and multiline test fixtures;
- target-typed `new`, `nameof`, null-coalescing, and null-conditional assignment where unambiguous;
- C# 14 field-backed properties when accessor validation is clearer than a named backing field;
- extension members when they express a stable operation owned neither by the receiver nor a domain service.

Do not compress branching, validation, or error handling into dense expressions. Avoid primary constructors when parameter lifetime or field capture would be surprising. Avoid extension methods or members as a substitute for proper domain behavior.

## Types and contracts

- Default concrete classes to `sealed` when inheritance is not part of the design.
- Prefer immutable cross-layer messages and domain value objects.
- Use domain-specific types instead of repeatedly passing primitive strings or integers with hidden rules.
- Keep public APIs small. Use `internal` unless a type must cross an assembly boundary.
- Use named types when tuples or positional records would make call sites ambiguous.
- Validate at the earliest responsible boundary and keep domain invariants inside Domain.
- Keep types cohesive and dependencies narrow. If a constructor needs many unrelated services, split the responsibility instead of hiding them behind a facade.
- Prefer composition over inheritance. Use inheritance only for a genuine substitutable relationship with a stable base contract.
- Do not create an interface solely to mirror every concrete class; create it at a consumer-owned boundary or meaningful substitution seam.

## Async and cancellation

- Use true asynchronous APIs for network, filesystem, serialization, and database I/O.
- Name asynchronous methods with `Async`, except framework-defined handlers where convention says otherwise.
- Accept a `CancellationToken` at application and infrastructure I/O boundaries and propagate it to every cancellable operation.
- Do not use `.Result`, `.Wait()`, `Task.Run` around I/O, `async void`, or fire-and-forget work without an owned background-work mechanism.
- Return `Task` by default. Use `ValueTask` only when the completion profile and consumption rules justify it.
- Do not add `ConfigureAwait(false)` mechanically in ASP.NET Core application code.

## Errors, HTTP, and validation

- Use exceptions for exceptional infrastructure failures and explicit results for expected business outcomes when callers must branch on them.
- Preserve useful causal context when translating errors; do not expose secrets or vendor internals to clients.
- Map API failures consistently to `ProblemDetails` or typed HTTP results.
- Avoid catch-all blocks that log and continue with corrupt or ambiguous state.
- Validate external input and bound request/media sizes before expensive processing.

## Dependency injection and configuration

- Prefer constructor injection and small dependency sets.
- Bind related settings to validated options classes; validate required configuration at startup.
- Use `IHttpClientFactory` typed clients for external services and keep base addresses, timeouts, and resilience policies at registration boundaries.
- Do not use service location or inject `IServiceProvider` into business code.
- Keep DI registration grouped by layer through clear extension methods.

## Naming and documentation

- Choose domain vocabulary and intention-revealing names; avoid `Helper`, `Manager`, `Processor`, and `Util` without a precise responsibility.
- Comments explain constraints or decisions, not syntax.
- Add XML documentation to real public APIs and non-obvious contracts, not every private member.
- Remove dead code, stale comments, unused imports, and obsolete suppression while touching the area.

## Razor and Blazor

- Keep components focused on presentation and interaction state.
- Move reusable business workflows to Application and HTTP concerns to typed Web clients.
- Dispose JavaScript modules, subscriptions, timers, and cancellation sources deterministically.
- Avoid unnecessary renders and large component state, but prefer clear state flow over premature micro-optimization.
