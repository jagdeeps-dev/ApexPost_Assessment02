# ApexPost Logistics — Assessment 02

ApexPost is a single-process ASP.NET Core MVC counter platform for postal retail, shipping and money-order workflows. This repository is organized as a layered .NET 10 / C# 14 solution with JSON-file persistence, custom cookie authentication, request middleware, MVC action filters, audit logging, and unit/integration tests.

> **Assessment status:** Documentation and source structure have been reviewed against the project included in this ZIP. This does not mean the solution has been built or all tests passed in this environment. Run the verification commands below on a machine with the .NET 10 SDK before submission.

## Contents

- `ApexPost.sln` — traditional Visual Studio solution; recommended entry point.
- `ApexPost.slnx` — XML solution format, retained as an alternative.
- `src/ApexPost.Domain` — entities, value objects, business rules and language-feature examples.
- `src/ApexPost.Application` — application contracts, DTOs, services, reports and validation.
- `src/ApexPost.Infrastructure` — JSON repositories, audit persistence, password hashing, cache, notifications and seed data.
- `src/ApexPost.Web` — MVC controllers/views, middleware, action filters, authorization and static assets.
- `tests/ApexPost.UnitTests` — domain/business-rule tests.
- `tests/ApexPost.IntegrationTests` — ASP.NET Core `WebApplicationFactory` security-pipeline tests.
- `DESIGN.md` — architecture, pipeline, security, performance decisions and known limitations.
- `CHECKLIST.md` — assessment checklist with implementation versus verification status.
- `ASSESSMENT-TRACEABILITY.md` — requirement-to-code/test evidence matrix.
- `docs/adr/` — eight Architecture Decision Records.
- `AUDIT-SAMPLE.json` — illustrative audit record shape; not runtime evidence.
- `.github/workflows/dotnet.yml` — CI workflow.

## Assessment marks mapping

| Assessment area | Marks | Main evidence |
|---|---:|---|
| Domain model and business rules | 20 | `src/ApexPost.Domain`, application workflows, unit tests |
| Advanced C# 14 | 15 | `src/ApexPost.Domain/Rules/LanguageFeatures.cs`, value objects and application code |
| ASP.NET Core MVC | 10 | `src/ApexPost.Web/Controllers`, `Views`, model validation and POST/redirect flows |
| Middleware, security and observability | 20 | `Program.cs`, `Middleware/PipelineMiddleware.cs`, authorization and integration tests |
| Action filters and audit | 15 | `Filters/ActionFilters.cs`, `IAuditStore`, `JsonAuditStore` |
| Performance and concurrency | 10 | async JSON I/O, cancellation, `Task.WhenAll`, `SemaphoreSlim`, rate cache, timeouts and rate limiting |
| Tests, documentation, ADRs and Git | 10 | unit/integration test projects, this README, `DESIGN.md`, checklist, traceability and ADRs |
| **Total** | **100** | Evidence must be verified on the target machine; no mark guarantee is implied. |

## Prerequisites

- .NET 10 SDK.
- Visual Studio with the .NET desktop/web development workload, or the .NET CLI.
- A local writable data directory for JSON files.
- Configure demo passwords and integration secrets through User Secrets or environment variables for local testing. Do not commit real credentials.

## Build and test

From the repository root (`ApexPost/`):

```bash
dotnet --info
dotnet restore ApexPost.sln
dotnet build ApexPost.sln -c Release
dotnet test ApexPost.sln -c Release --logger "console;verbosity=normal"
```

The project also contains `ApexPost.slnx`; use one solution file consistently during a given build. If your Visual Studio installation does not support `.slnx`, use `ApexPost.sln`.

The current source contains **8 unit-test cases** and **8 integration-test cases**. The integration suite currently covers security headers, missing API key, blocked IP, login rate limiting, unauthenticated redirect, clerk void authorization, maintenance/health behavior and correlation ID. The separate scenario “server API key is not configured returns 500” should be added and verified; it is not present in the current `SecurityPipelineTests.cs` inspected for this package.

## Run locally

1. Set the demo passwords using User Secrets or environment variables. The configuration keys used by the seeder are `DemoPasswords:CounterClerk`, `DemoPasswords:BranchManager`, `DemoPasswords:Auditor`, and `DemoPasswords:Administrator`.
2. Review `src/ApexPost.Web/appsettings.json` and ensure the data directory and integration settings are suitable for your machine.
3. Start the web project:

   ```bash
   dotnet run --project src/ApexPost.Web/ApexPost.Web.csproj
   ```

4. Use the configured HTTPS URL from the launch profile.
5. For first-run local demo data, the seeder creates 5 branches, 20 staff, 30 products, 10 shipping zones with service-level rates, and 40 historical transactions. Existing data is preserved rather than reset automatically.

Do not rely on demo credentials in a shared or production environment. Configure unique secrets and a suitable IP allowlist before exposing the application.

## Security notes

- Authentication uses a custom ASP.NET Core cookie scheme; ASP.NET Core Identity is not used.
- Passwords are hashed with PBKDF2 by the infrastructure password service.
- Integration API keys are read from configuration and compared with a constant-time comparison. Never put real keys in source control.
- IP allowlisting is independent from API-key validation. A blocked client can receive `403` before API-key validation runs; an allowed client with no API key should receive `401`; an unconfigured server key should receive `500`.
- CORS is not authentication. Non-browser clients can ignore CORS.
- If deployed behind a reverse proxy, configure trusted forwarded headers explicitly. Do not trust arbitrary forwarded IP headers.
- Audit records must not contain passwords, API keys, full identity-document numbers, or request bodies.

## Verification before submission

- [ ] Release build succeeds with no unexplained warnings.
- [ ] All unit and integration tests pass.
- [ ] Add/run the missing server-key-not-configured (`500`) integration test.
- [ ] Manually verify branch/staff workflows, mixed basket payment, money-order verification, void/refund authorization and all four reports.
- [ ] Verify seed counts on a clean data directory.
- [ ] Inspect generated audit JSONL and confirm redaction.
- [ ] Verify rate limiting, CORS, maintenance-mode health bypass and IP allowlist behavior.
- [ ] Check `git status`, inspect the final diff and ensure `.git`, `.vs`, `bin`, `obj`, runtime data and secrets are not included in the submitted source archive.

## Known limitations

- JSON persistence and in-process locks are suitable for the assessment's single-process scope, not multi-instance financial transactions.
- Multi-file operations cannot be fully atomic across JSON files.
- Courier status and identity-verification behavior are simulations, not live external integrations.
- Runtime build/test results must be recorded from the target machine; this documentation does not claim a verified 100/100 result.
