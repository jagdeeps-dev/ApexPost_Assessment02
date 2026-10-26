# Assessment 02 Completion Checklist

Status convention: `[x]` means the implementation or documentation is authored in this ZIP; `[ ]` means target-machine verification or manual acceptance remains. No build/test item is claimed as verified because the .NET SDK was unavailable in the authoring environment.

## 1. Domain and business rules — 20 marks
- [x] ApexPost domain replaces RMS domain in the solution.
- [x] Branches, counters, staff, roles, products, branch stock, shipments, money orders, identity verifications, mixed transactions and payments are modeled.
- [x] Business rules exist for non-negative stock, active staff/branch, assigned branch, high-value verification, shipping weight, and manager-only void/refund with reason.
- [x] Four required report services/views are present.
- [x] Simulated notifications use events/observers.
- [ ] Verify seed counts after first run: 5+ branches, 20+ staff, 30+ products, 10+ zones/rates, 40+ historical transactions.
- [ ] Verify branch/staff management, mixed retail/shipping basket, payment, high-value money order, void and refund end to end.

## 2. Advanced C# 14 — 15 marks
- [x] Records/value objects, pattern matching, extension members and `field` keyword.
- [x] Generic repository constraint and report specification.
- [x] `Func`/`Predicate` business rules, events and cached reflection validator.
- [x] Async/cancellation, `Task.WhenAll`, `SemaphoreSlim`, `ValueTask` cache and timeout handling.
- [ ] Compile with .NET 10/C# 14 and confirm extension-member / `field` syntax compiles.
- [ ] Add measured performance results if making throughput claims.

## 3. MVC — 10 marks
- [x] Login/logout, dashboard, catalogue/stock, transaction, shipping preview, money orders, identity verification and report views.
- [x] Branch/staff management and stock adjustment views.
- [x] MVC POST actions use antiforgery and redirect after successful operations where appropriate.
- [ ] Browser-test all form validation, PRG flows, result codes, 404 and 403 behavior.
- [ ] Verify accessible layout and responsive rendering in target browser.

## 4. Middleware/security/observability — 20 marks
- [x] Problem Details exception middleware, correlation ID, request timing/logging, maintenance mode, security headers, HTTPS redirect and HSTS outside Development.
- [x] Endpoint-metadata API key validation with constant-time comparison and 401/500 behavior.
- [x] Configured IP allowlist for admin/integration and documented forwarded-header risks.
- [x] Cookie auth with StaffId/Role/BranchCode claims; role and same-branch policies.
- [x] Login and partner rate limits; restricted partner CORS; `/health` and `/health/live`.
- [ ] Run all middleware integration tests and manually verify maintenance mode / CORS / API-key-not-configured responses.
- [ ] Review production allowlist, proxy trust, secret manager and HTTPS configuration.

## 5. Action filters and auditing — 15 marks
- [x] Async audit, validation and performance filters registered globally.
- [x] Audit uses `IAuditStore` and JSON Lines append storage; route values are allowlisted.
- [x] Sensitive fields are not serialized into audit records.
- [ ] Inspect runtime `AuditRecord.jsonl`; confirm actor, role, branch, action, result, duration, timestamp and correlation ID.
- [ ] Verify audit failure monitoring and audit retention/backup expectations.

## 6. Performance — 10 marks
- [x] Async JSON I/O, cancellation propagation, bounded stock writes and courier concurrency, rate cache, parallel report reads and rate limiting.
- [ ] Confirm no `.Result`, `.Wait()` or `Thread.Sleep` in application source.
- [ ] Run concurrent stock tests and confirm no negative quantity or lost updates.
- [ ] Capture timing/load measurements before claiming performance gains.

## 7. Tests, docs, ADRs and Git — 10 marks
- [x] Domain unit test source covers stock, money-order verification, parcel weight and pricing plus additional rules.
- [x] Nine `WebApplicationFactory` integration tests are authored.
- [x] README, DESIGN, traceability matrix, this checklist, eight ADRs, audit sample and CI workflow are included.
- [ ] Run Release build and full tests; resolve all failures and warnings.
- [ ] Inspect test counts and coverage report; do not claim 100% without measurement.
- [ ] Verify no secrets, runtime data, `bin/`, `obj/`, `.vs/` or local files are tracked.
- [ ] Create feature branch and pull request; review actual diff.

## Final sign-off
- [ ] Build succeeds with zero warnings on .NET 10.
- [ ] All tests pass on target machine.
- [ ] Manual acceptance tests pass.
- [ ] All 100-mark rubric rows have evidence recorded.
- [ ] Remaining limitations are disclosed in README/DESIGN.
