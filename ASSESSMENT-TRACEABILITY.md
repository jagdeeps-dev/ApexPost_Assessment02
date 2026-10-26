# Assessment 02 Requirement Traceability Matrix

**Evidence status:** This matrix maps the inspected source to assessment areas. “Present in source” is not equivalent to a passing build or accepted behavior. No runtime verification is claimed by this package.

| Requirement / marks area | Source evidence | Verification still required | Status |
|---|---|---|---|
| Domain/business rules — 20 | `src/ApexPost.Domain/Primitives/Primitives.cs`; `Rules/LanguageFeatures.cs`; application services; `tests/ApexPost.UnitTests/Domain/BusinessRuleTests.cs` | Build and execute tests; end-to-end workflows for stock, branch status, money orders, mixed transactions, void/refund | Source present; runtime pending |
| Seed data and reports | `src/ApexPost.Infrastructure/SeedData/AppSeeder.cs`; `src/ApexPost.Application/Reports/ReportService.cs`; `src/ApexPost.Web/Controllers/ReportsController.cs` and report views | Verify 5 branches, 20 staff, 30 products, 10 zones/rates, 40 historical transactions; validate four report outputs | Source present; runtime pending |
| Advanced C# 14 — 15 | `Primitives.cs`, `Rules/LanguageFeatures.cs`, application and infrastructure services | Build using .NET 10/C# 14; verify examples are used and warnings are addressed | Source present; compiler verification pending |
| MVC — 10 | `src/ApexPost.Web/Controllers/`, `Views/`, `Models/`, `Program.cs` | Browser test form validation, antiforgery, PRG, 404/403 and branch scoping | Source present; manual verification pending |
| Middleware/security/observability — 20 | `src/ApexPost.Web/Middleware/PipelineMiddleware.cs`; `Program.cs`; `Security/SameBranchAuthorization.cs` | Execute security integration tests; verify middleware order, API key and IP behavior, maintenance, CORS, rate limiting and health endpoints | Source present; runtime pending |
| API key not configured | `ApiKeyMiddleware` handles empty configuration with 500 in source | Add/run integration test with an allowed client and empty `Integration:ApiKey`; ensure IP middleware does not short-circuit first | Test evidence missing in current inspected suite |
| Action filters/audit — 15 | `src/ApexPost.Web/Filters/ActionFilters.cs`; `src/ApexPost.Infrastructure/Audit/JsonAuditStore.cs`; `AUDIT-SAMPLE.json` | Inspect runtime JSONL, redaction, actor/result/correlation fields and failure handling | Source present; runtime pending |
| Performance — 10 | async JSON repository, cancellation, `Task.WhenAll`, `SemaphoreSlim`, `RateCache`, courier timeout/concurrency and rate limiting | Search blocking calls, run concurrent stock and load tests, capture measurements | Source present; measurements pending |
| Tests/docs/ADRs/Git — 10 | 8 unit tests, 8 integration tests, README, DESIGN, checklist, traceability, 8 ADRs, CI workflow | `dotnet restore`, Release build, `dotnet test`, inspect CI results and final Git diff | Source/docs present; runtime pending |

## Required target-machine verification

From the repository root:

```bash
dotnet restore ApexPost.sln
dotnet build ApexPost.sln -c Release
dotnet test ApexPost.sln -c Release --logger "console;verbosity=normal"
```

Then manually verify:
1. First-run seed counts and existing-data preservation.
2. Staff login/logout and role authorization.
3. Branch isolation and closed/inactive entity handling.
4. Retail/shipping mixed transaction and payment.
5. High-value money-order identity verification linked to the same transaction.
6. Manager-only void/refund with required reason.
7. All four reports.
8. Security headers, correlation ID, exception handling, maintenance/health, API key 401/500, blocked IP 403, rate-limit 429 and partner CORS.
9. Audit JSONL redaction and record fields.
10. No secrets or build/runtime artifacts in Git/package.

Record actual command output before marking any row runtime-verified.
