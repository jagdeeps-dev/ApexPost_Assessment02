# Assessment 02 Requirement Traceability Matrix

Status key: **Implemented in source** means an implementation has been authored; **Needs runtime verification** means the target SDK build/test/manual acceptance still must be performed. Since this authoring environment has no .NET SDK, no row is marked runtime-verified.

| Requirement / marks area | Planned/implemented location | Evidence / verification | Status |
|---|---|---|---|
| Branches, counters, staff and roles | `Domain/Primitives.cs`; `BranchesController`; `StaffController`; seed initializer | Domain checks; admin role smoke test; seed counts | Implemented; runtime verification pending |
| Product catalogue and per-branch stock | `Product`, `StockItem`; `ProductsController`; `InventoryController` | Stock never-negative unit test; stock-adjustment negative test | Implemented; runtime verification pending |
| Shipping price strategy and max weight | `IShippingPricingStrategy`; `ZoneRateShippingPricingStrategy`; `BusinessRules` | `BusinessRuleTests` weight/pricing cases | Implemented; runtime verification pending |
| Money orders + same-transaction identity verification | `MoneyOrdersController`; `MoneyOrder`; `IdentityVerification`; `CounterService` | Domain threshold test; manual high-value workflow; inspect persisted links and masked number | Implemented; runtime verification pending |
| Mixed transaction basket, tax and payment | `CounterTransaction`; `CounterService`; transaction views | totals/lifecycle unit tests; add retail + shipping + payment browser test | Implemented; runtime verification pending |
| Voids/refunds manager-only + reason | `TransactionsController`; `BusinessRules.EnsureManager` | clerk-forbidden integration test; manager success/failure manual tests | Implemented; runtime verification pending |
| Four reports | `ReportService`; `ReportsController`; report views | report calculation tests/manual seeded-data review | Implemented; runtime verification pending |
| JSON repositories and async I/O | `JsonRepository<T>`; `JsonAuditStore` | Add infrastructure tests for malformed JSON, missing file, concurrent writes and cancellation | Implemented; additional tests recommended |
| At least 8 advanced C# 14 features (15 marks) | `Primitives.cs`, `LanguageFeatures.cs`, Application/Infrastructure services | Feature matrix in `DESIGN.md`; build with .NET 10/C# 14 | Implemented in source; compiler verification pending |
| MVC pages and PRG (10 marks) | `Views/`, controllers | Browser acceptance test for each POST and correct result | Implemented; runtime verification pending |
| Exception, correlation, timing, maintenance, headers (middleware 20 marks) | `Middleware/PipelineMiddleware.cs` | Header/correlation integration tests; maintenance toggle manual test | Implemented; runtime verification pending |
| API key, IP allowlist, cookies, roles, antiforgery, rate limiting, CORS | `Program.cs`, middleware, `AccountController`, `IntegrationController` | 401, 403, 429, login redirect and clerk void tests; CORS manual test | Implemented; runtime verification pending |
| Audit, validation, performance filters (15 marks) | `Filters/ActionFilters.cs`; `JsonAuditStore` | Inspect generated `AuditRecord.jsonl`; verify redaction and slow-action warning | Implemented; runtime verification pending |
| Async, Task.WhenAll, SemaphoreSlim, cache, timeouts (10 marks) | Application/Infrastructure/Controllers | Load/concurrency test and measured baseline recommended | Implemented; measurements pending |
| Unit + at least 6 integration tests; docs/ADRs/Git (10 marks) | `tests/`, README, DESIGN, ADRs, workflow | `dotnet test -c Release`; Git diff/secret scan; inspect actual test output | Test sources authored; runtime verification pending |
| Seed minimum counts | `Infrastructure/SeedData.cs` | Inspect JSON counts after clean local startup | Implemented in source; runtime verification pending |
| Exact deliverables | Root documentation and `AUDIT-SAMPLE.json` | File existence and checklist review | Implemented in source; runtime verification pending |

## Required target-machine acceptance run

1. `dotnet restore ApexPost.slnx`
2. `dotnet build ApexPost.slnx -c Release`
3. `dotnet test ApexPost.slnx -c Release --logger "console;verbosity=normal"`
4. Run the Web app with secrets configured; verify seeded counts and role accounts.
5. Manually verify PRG, branch isolation, closed branch, inactive staff, stock concurrency, high-value money-order verification link, void/refund, maintenance bypass for health, CORS, API key 401/500, blocked IP 403 and rate-limit 429.
6. Review all warnings, generated audit entries, JSON privacy, Git status and secret scan.
7. Attach actual build/test/coverage output before claiming complete verification.
