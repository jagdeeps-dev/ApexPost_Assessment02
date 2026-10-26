# Assessment 02 Completion Checklist

**Status convention:** `[x]` means the feature or documentation is present in the inspected source. `[ ]` means runtime verification, manual acceptance, or further evidence is still required. Source presence is not proof that a requirement works correctly.

## 1. Domain and business rules — 20 marks

- [x] Domain entities/value objects cover branches, counters, staff/roles, products, branch stock, shipment rates, shipments, money orders, identity verifications, transactions and payments.
- [x] Business-rule code covers non-negative stock, active staff/branch and branch assignment, high-value money-order verification, shipping weight/pricing, and manager-only void/refund with reason.
- [x] Four report actions/views are present: daily sales, low stock, shipping volume and money-order audit.
- [x] In-process notification events are defined and connected to structured logging.
- [ ] On a clean data directory, verify 5 branches, 20 staff, 30 products, 10 zones/rate sets and 40 historical transactions.
- [ ] Manually verify mixed retail/shipping basket, payment, high-value money order + same-transaction verification, void and refund.
- [ ] Verify closed branch, inactive staff, cross-branch access and insufficient stock paths.

## 2. Advanced C# 14 — 15 marks

- [x] Source includes records/value objects, pattern matching, C# 14 extension members, the `field` keyword, generic constraints, specifications, delegates/predicates, events, reflection-based validation, async/cancellation, `Task.WhenAll`, `SemaphoreSlim`, `ValueTask` cache and timeout handling.
- [ ] Build with .NET 10 SDK/C# 14 and confirm the extension-member and `field` syntax compiles.
- [ ] Confirm feature examples are used meaningfully and are not just dead demonstration code.
- [ ] Do not claim measured performance improvements without repeatable measurements.

## 3. ASP.NET Core MVC — 10 marks

- [x] Login/logout, dashboard, catalogue/stock, transactions, shipping preview, money orders, identity verification, management and report controllers/views are present.
- [x] MVC filters are registered globally.
- [ ] Browser-test validation messages, antiforgery, POST/redirect/GET, result pages, 404 and forbidden paths.
- [ ] Verify role/branch scoping for each management and reporting action.
- [ ] Check responsive/accessibility basics in the target browser.

## 4. Middleware, security and observability — 20 marks

- [x] Correlation ID, global exception handling/Problem Details, request timing, security headers, maintenance mode, HTTPS redirection and HSTS outside Development are implemented in source.
- [x] Endpoint-metadata API-key validation, constant-time comparison and server-configuration failure handling are implemented.
- [x] IP allowlisting, cookie authentication, role/policy authorization, partner CORS, login/partner rate-limit policies and health endpoints are configured.
- [ ] Run the complete integration suite and verify actual response codes on the target machine.
- [ ] Add an integration test proving an allowed client receives 500 when the server API key is unconfigured. This scenario is discussed in the design but is absent from the currently inspected test source.
- [ ] Verify `401` for missing key when IP is allowed, `403` for blocked IP, `429` for rate limit and `503` during maintenance (while health remains available).
- [ ] Review production secrets, IP allowlist and trusted reverse-proxy configuration.

## 5. Action filters and audit — 15 marks

- [x] Async audit, validation and performance action filters are registered.
- [x] Audit storage uses `IAuditStore` and append-only JSON Lines.
- [x] Audit route values are allowlisted; sensitive request bodies/secrets should not be serialized.
- [ ] Inspect a runtime-generated audit file and verify actor, role, branch, action, result, duration, timestamp and correlation ID.
- [ ] Test audit-store failure behavior and document retention/backup expectations.

## 6. Performance and concurrency — 10 marks

- [x] Async JSON I/O and cancellation tokens are used in the repository/application flows.
- [x] Source uses `Task.WhenAll`, `SemaphoreSlim`, a rate cache, bounded courier concurrency, timeouts and rate limiting.
- [ ] Search application source for `.Result`, `.Wait()` and `Thread.Sleep`; review every match.
- [ ] Run concurrent stock mutation tests and check for negative quantities/lost updates.
- [ ] Capture repeatable latency/load measurements if performance claims are included.

## 7. Tests, documentation, ADRs and Git — 10 marks

- [x] Unit test project contains 8 `[Fact]` cases in the inspected source.
- [x] Integration test project contains 8 `[Fact]` cases in the inspected source.
- [x] `README.md`, `DESIGN.md`, `ASSESSMENT-TRACEABILITY.md`, this checklist, eight ADRs, audit sample and CI workflow are included in this reviewed package.
- [ ] Run Release build and full test suite; fix all failures and investigate warnings.
- [ ] Add the missing API-key-configuration failure integration test and any rubric-required gaps revealed by the official assessment sheet.
- [ ] Record actual build/test output and coverage if measured; do not claim 100% coverage without evidence.
- [ ] Confirm no secrets, runtime data, `.git/`, `.vs/`, `bin/` or `obj/` are part of the submitted source archive.
- [ ] Review branch, commits, pull request and final Git diff.

## Final sign-off

- [ ] Release build succeeds on .NET 10.
- [ ] All unit and integration tests pass.
- [ ] Manual acceptance tests pass.
- [ ] Every rubric row has concrete evidence.
- [ ] Remaining limitations are disclosed.
- [ ] Only after all of the above, estimate marks against the official assessor rubric.
