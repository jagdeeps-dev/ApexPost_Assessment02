# ApexPost Logistics — Week 2 Counter Platform

A layered .NET 10 / C# 14 ASP.NET Core MVC demonstration for a postal retail counter. This solution is scoped to the supplied Assessment 02 brief: JSON persistence only, no database/ORM, cookie authentication implemented without ASP.NET Core Identity, auditable counter workflows, security middleware, action filters, tests, and documented architectural decisions.

> **Assessment note:** This is a prepared implementation baseline. The current authoring environment did not have the .NET SDK installed, so a real `dotnet restore`, build, test run, browser acceptance test, and coverage report could not be executed here. Do not treat the solution as verified until the commands in this README pass on a machine with the .NET 10 SDK. The project intentionally does not claim a guaranteed 100/100 score.

## Contents

- `src/ApexPost.Domain` — domain entities, value objects, business invariants, pricing strategy, policies, events, and C# 14 language-feature examples.
- `src/ApexPost.Application` — use-case services, repository contracts, report contracts, business-use-case abstractions, and shared concurrency coordination.
- `src/ApexPost.Infrastructure` — asynchronous JSON repositories, append-only JSON Lines audit store, PBKDF2 password hashing, seed data, rate cache, and bounded simulated courier integration.
- `src/ApexPost.Web` — MVC controllers/views, cookie authentication, authorization policies, middleware, action filters, health endpoints, and responsive UI.
- `tests/ApexPost.UnitTests` — domain unit tests.
- `tests/ApexPost.IntegrationTests` — `WebApplicationFactory` security integration tests.
- `DESIGN.md`, `CHECKLIST.md`, `ASSESSMENT-TRACEABILITY.md`, and `docs/adr` — design rationale and marking evidence checklist.

## Prerequisites

- .NET 10 SDK (C# 14)
- Visual Studio 2026 or an editor with .NET 10 support
- Git

No database, ORM, or third-party runtime package is required by the application. Test packages are declared centrally in `Directory.Packages.props`.

## Configure local demo passwords and integration secret

Do this **before the first run**. Passwords are never committed to the repository; seeded demo users store PBKDF2-SHA256 password hashes only. Set distinct passwords of at least 12 characters for each role from the Web project directory:

```powershell
cd src/ApexPost.Web
dotnet user-secrets set "DemoPasswords:CounterClerk" "Use-a-unique-clerk-password-123!"
dotnet user-secrets set "DemoPasswords:BranchManager" "Use-a-unique-manager-password-123!"
dotnet user-secrets set "DemoPasswords:Auditor" "Use-a-unique-auditor-password-123!"
dotnet user-secrets set "DemoPasswords:Administrator" "Use-a-unique-admin-password-123!"
dotnet user-secrets set "Integration:ApiKey" "replace-with-a-long-random-integration-key"
dotnet user-secrets set "Integration:PartnerWidgetOrigin" "https://your-approved-tracking-widget.example"
```

The example values above are placeholders for local setup; replace them with private values. Never use these example strings as production credentials.

### Demo account names

The seed initializer creates 20 staff across the four roles. The first seeded account for each role is:

| Role | Email | Password configuration key |
|---|---|---|
| Counter Clerk | `counterclerk1@apexpost.local` | `DemoPasswords:CounterClerk` |
| Branch Manager | `branchmanager2@apexpost.local` | `DemoPasswords:BranchManager` |
| Auditor | `auditor3@apexpost.local` | `DemoPasswords:Auditor` |
| Administrator | `administrator4@apexpost.local` | `DemoPasswords:Administrator` |

Seeded accounts with no configured password cannot sign in. If the app was run before passwords were configured, set the user-secrets values and restart; the initializer upgrades only the `UNCONFIGURED` password fields and preserves the rest of the JSON data. For a disposable local environment only, you can delete the configured development data directory to recreate all seed records. **Back up data before deleting anything.**

## Build, test, and run

From the repository root:

```bash
dotnet --info
dotnet restore ApexPost.sln
dotnet build ApexPost.sln --configuration Release
dotnet test ApexPost.sln --configuration Release --logger "console;verbosity=normal"
dotnet run --project src/ApexPost.Web/ApexPost.Web.csproj
```

The launch profile uses `https://localhost:7244` and `http://localhost:5244`. Prefer HTTPS because the authentication cookie is configured `Secure` and is not sent over plain HTTP. If HTTPS development certificates are not trusted, run `dotnet dev-certs https --trust` on your development machine.

## Data and audit storage

- JSON data is written to `src/ApexPost.Web/App_Data` when run from the Web project directory. Set `Data:Directory` to an absolute path for a different location.
- Files are created lazily by the repository. Writes use per-file in-process semaphores, a temporary file and atomic replacement. Malformed JSON is not silently reset.
- The seed initializer creates at least 5 branches, 20 staff, 30 products, 10 destination zones (three service-level rates per zone), and 40 historical counter transactions.
- Audit events are appended as JSON Lines to `AuditRecord.jsonl`. `AUDIT-SAMPLE.json` is a sanitised example, not a claim that it was generated by a runtime in this authoring environment.
- JSON repositories and their locks support a **single application process**. They are not a multi-instance database and do not guarantee atomic commits across multiple files. This limitation and a migration path are documented in `DESIGN.md` and ADRs.

## Security configuration

- API integration key: `Integration:ApiKey` via user-secrets in Development or a secret manager/environment variable in deployment.
- Partner widget origin: `Integration:PartnerWidgetOrigin`; only that origin is allowed by the CORS policy. For separate partners, set `Integration:Partners:<partnerId>:ApiKey` and send that ID in `X-Partner-Id`; each validated partner receives its own rate-limit partition. The single `Integration:ApiKey` setting is the local/default partner fallback.
- Admin/integration IP allowlist: `Security:AllowedIps`. The checked-in loopback values are for local development only; replace them for a real environment and set `AllowedHosts` to the intended host names.
- Maintenance mode: `Maintenance:Enabled` and `Maintenance:RetryAfterSeconds`.
- Money-order identity threshold: `MoneyOrders:IdentityThreshold`.
- Low-stock threshold: `Inventory:LowStockThreshold`.
- Slow action warning threshold: `Performance:ActionWarningThresholdMs`.

Example environment variable names use the double-underscore convention, e.g. `Integration__ApiKey` and `Security__AllowedIps__0`. Do not commit `.env` files, secrets, real identity documents, or production data.

## Main workflows

- Login and logout with claims for `StaffId`, role and branch code.
- Branch dashboard and product/stock view.
- Start a counter transaction; add retail items and shipping services; take payment.
- Shipping price preview using zone, weight, shipment kind and service-level strategy.
- Money orders can be issued standalone or added to an open mixed basket; payment moves pending orders to Issued, and void/refund cancels linked orders. High-value verification is linked to the same counter transaction. A separate identity-verification form records successful/failed results against an open transaction.
- Manager-only transaction void/refund with a required reason.
- Branch/staff administration and stock adjustment.
- Four reports: daily sales split by retail/services, low stock, shipping volume by zone/service, and money-order/identity-verification counts for auditors.
- Partner parcel-status endpoint: `GET /integration/parcel-status/{shipmentId}` with `X-Api-Key` and `X-Partner-Id` (omit partner ID only for the configured `default` key).

Example request after configuring the local secret:

```bash
curl --include \
  -H "X-Api-Key: $APEXPOST_API_KEY" \
  -H "X-Partner-Id: default" \
  "https://localhost:7244/integration/parcel-status/<shipment-id>"
```

- Health endpoints: `/health` and `/health/live` remain available during maintenance mode.

## Test coverage and final acceptance

The test project contains the four required business-rule categories and additional domain tests, plus nine integration scenarios covering security headers, API-key rejection and missing-key configuration, blocked IP, login throttling, unauthenticated redirect, clerk-forbidden void, correlation ID echo, and maintenance/health behavior. Run the test suite on the target SDK and record the actual result. Add/adjust tests for any behavior that fails in the target environment. Do not state 100% coverage unless a coverage tool was run and its report confirms it.

## Git workflow

Use feature branches, small commits and pull requests with explicit titles, for example `feature/apexpost-domain` or `security/api-key-ip-restrictions`. Review `git status`, generated files, secrets, test output and the diff before committing. The included GitHub Actions workflow restores, builds and tests the solution on .NET 10.
