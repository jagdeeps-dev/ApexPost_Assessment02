# ADR-008: Bounded Concurrency, Async I/O and Cache

- **Status:** Accepted
- **Context:** The app should stay responsive, avoid blocking calls and protect shared JSON files/stock updates. Courier integrations are simulated.
- **Decision:** Propagate cancellation tokens, use `Task.WhenAll` for independent dashboard/report reads, `SemaphoreSlim` for same-process file and stock operations and a four-call courier concurrency limit. Cache zone rates using `ValueTask` for the hot path. Apply login and partner rate limits and explicit timeouts to simulated lookups.
- **Consequences:** These mechanisms improve safety and boundedness for a single process. They are not distributed locks or distributed rate limits. Cache benefits must be measured before being claimed; database concurrency controls are required for multi-instance deployment.
