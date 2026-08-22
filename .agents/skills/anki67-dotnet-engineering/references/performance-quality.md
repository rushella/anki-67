# Performance and quality

Use this reference for I/O, serialization, concurrency, caching, media, external providers, hot loops, and tests.

## Performance decision order

1. Establish correctness and workload constraints.
2. Choose an efficient algorithm and avoid unnecessary remote calls.
3. Bound data, concurrency, time, retries, and memory.
4. Measure representative behavior with traces, metrics, or benchmarks when performance matters.
5. Optimize the measured bottleneck and retain a regression test or benchmark when practical.

Readable O(n) code usually beats intricate allocation tricks around an avoidable O(n²) operation.

## I/O and concurrency

- Use asynchronous streaming or incremental processing when inputs may be large; do not buffer entire media or responses without a size bound.
- Propagate cancellation and set explicit external-service timeouts.
- Parallelize independent I/O only with bounded concurrency and deterministic result handling.
- Avoid holding locks across `await`. Prefer ownership, immutability, channels, or framework concurrency primitives over hand-rolled synchronization.
- Make retries selective, bounded, observable, and safe for the operation's idempotency.
- Use deterministic media names or content hashes to avoid duplicate storage and work.

## Allocations and hot paths

- Avoid repeated enumeration, unnecessary `ToList`/`ToArray`, boxing, intermediate strings, and closure capture in confirmed hot paths.
- Prefer direct collection lookup over repeated linear searches where scale warrants it.
- Use `Span<T>`, `ReadOnlySpan<T>`, `Memory<T>`, `ArrayPool<T>`, stack allocation, or ref semantics only when lifetimes are safe and measurement justifies the complexity.
- Prefer `StringBuilder` or direct span formatting for repeated concatenation in hot loops; ordinary interpolation is appropriate elsewhere.
- Seal non-extensible implementations and use static lambdas where natural, but do not claim meaningful gains without evidence.

## Serialization, logging, and HTTP

- Prefer `System.Text.Json`. Use source-generated serialization contexts for stable, high-throughput, trimming-sensitive, or AOT-facing contracts.
- Stream JSON and media where useful and enforce response-size limits.
- Use structured logging templates. Avoid interpolation that performs work before log-level checks.
- Use source-generated `LoggerMessage` methods for high-frequency logging paths.
- Reuse clients through `IHttpClientFactory`; do not construct and dispose `HttpClient` per request.
- Cache only data with a clear key, lifetime, invalidation policy, size limit, and ownership model.

## Testing strategy

- Unit tests are the default for every non-trivial deterministic Domain or Application behavior: branches, invariants, transformations, policies, use-case orchestration, and expected failures.
- Every bug fix adds a test that fails before the fix and passes after it whenever the failure can be reproduced locally.
- Domain: fast unit tests for invariants, value objects, and policy boundaries.
- Application: use-case tests with explicit fake ports; verify orchestration, cancellation, and expected failures.
- Infrastructure: integration or contract tests against realistic HTTP payloads, serialization, persistence, and provider errors.
- API: functional tests for routing, validation, CORS/authentication, status codes, and problem details.
- Web: component or client tests for meaningful presentation behavior; do not duplicate Application tests.
- Architecture: add dependency tests when necessary to prevent layer inversions.

Tests should describe externally observable behavior, cover edge boundaries, and remain deterministic. Prefer one behavior per test, focused fakes over mock-heavy interaction tests, and clear Arrange/Act/Assert structure. Use `TimeProvider` or an explicit clock port for time-sensitive behavior. Avoid sleep-based tests and unnecessary mocking of value objects.

Do not unit-test trivial property accessors, generated code, framework behavior, or DI registration line by line. Verify wiring with a composition or smoke test instead. If changed behavior lacks a useful automated test, state the exact constraint and perform the strongest available verification.

## Boy Scout rule

For every touched area:

- clarify misleading names;
- remove dead branches and duplication made obsolete by the change;
- fix nearby nullable/analyzer warnings;
- improve error and cancellation propagation;
- add the missing test that protects the behavior being changed;
- preserve unrelated user changes and avoid formatting churn outside scope.

Boy Scout cleanup is not authorization for a broad rewrite. If the correct local improvement exposes a larger migration, keep the seam clean and identify the follow-up explicitly.

## Verification

Run the narrowest meaningful checks first, then the solution-level checks proportional to risk:

- formatter or style verification for changed files;
- unit and integration tests for affected layers;
- `dotnet build` for the solution with no new warnings;
- functional smoke tests for changed HTTP/UI paths;
- `git diff --check` and review of the final diff for accidental churn.

Performance claims require evidence from a benchmark, trace, counter, allocation measurement, or a clearly demonstrated reduction in algorithmic/I/O work. State when an optimization is a design precaution rather than a measured improvement.
