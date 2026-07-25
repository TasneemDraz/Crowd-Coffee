# Architecture Notes

## Layering

`CrowdCoffee.Api` → `CrowdCoffee.Application` / `CrowdCoffee.Infrastructure` → `CrowdCoffee.Domain`

`Domain` has zero package or project references beyond the BCL. It owns entities, the order state machine (Epic 3), and the shared `Result`/`Error` types in `Domain/Common`, so that domain logic (e.g. an illegal state transition) can return a typed failure without depending on anything above it.

## Application layer pattern: plain classes, no MediatR

Each use case is a small, directly-injected class with a `Handle(...)` method (see `CrowdCoffee.Application.Diagnostics.EchoHandler`), not a MediatR command/handler pair. Reasoning: this is a small, mostly-solo-built backend, and MediatR's indirection (locate handler by generic type at runtime) doesn't pay for itself here — plain DI-resolved classes are easier to jump to from a controller and easier to step through in a debugger. Revisit only if the number of concurrent contributors or the need for generic cross-cutting behaviors (e.g. broad transactional wrapping across many handlers at once) grows enough to justify it.

## Validation

FluentValidation validators live in `Application` next to the request they validate (e.g. `EchoRequestValidator`). Controllers resolve the relevant `IValidator<T>` via DI, call `ValidateAsync` explicitly, and return `ValidationProblem(ModelState)` (400, RFC 9110 structured body) on failure before ever calling the handler. There's no MediatR pipeline behavior doing this automatically — it's one explicit call per action, which is what the "plain classes" decision above implies. `CrowdCoffee.Api.Controllers.DiagnosticsController` is the reference example.

## Error handling: Result over exceptions

Business-rule failures (a product out of stock, an illegal order transition, a validation failure) are returned as `Result` / `Result<T>` (`CrowdCoffee.Domain.Common`), not thrown. `CrowdCoffee.Api.Common.ResultExtensions.ToActionResult` maps a `Result<T>` to the right HTTP status from its `ErrorType` (Validation→400, NotFound→404, Conflict→409, Unauthorized→401, else→500).

Exceptions are reserved for truly unexpected failures (a broken DB connection, a bug). `GlobalExceptionHandlerMiddleware` is the last-resort net: it logs the exception via Serilog and returns a generic 500 `ProblemDetails` — it is not the primary error-handling path and should rarely fire in normal operation.

## Cross-cutting request pipeline (`Program.cs`, in order)

1. `GlobalExceptionHandlerMiddleware` — last-resort catch, outermost
2. `UseSerilogRequestLogging` — structured request/response logging
3. `CorrelationIdMiddleware` — reads/generates `X-Correlation-Id`, pushes it into the Serilog `LogContext` for the rest of the request

## Configuration

Typed options classes (e.g. `CrowdCoffee.Infrastructure.Configuration.DatabaseOptions`) bound via `IOptions<T>` from named config sections, rather than pulling raw strings out of `IConfiguration` at point of use. The local-dev `Database:ConnectionString` in `appsettings.json` uses Windows integrated auth, so it carries no secret and is safe to commit; production overrides it via environment variable.
