# ADR-002: JSON Persistence Behind Repository Interfaces

- **Status:** Accepted for Week 2
- **Context:** The assessment explicitly prohibits a database and ORM this week while requiring future database replacement without domain changes.
- **Decision:** Define generic `IRepository<T>` in Application and implement per-type JSON files in Infrastructure. Use asynchronous file I/O, cancellation, per-path semaphores, temp-file writes and atomic replacement. Corrupt data raises an error rather than being silently reset.
- **Consequences:** The single-process lab is simple and portable. Locks do not coordinate multiple processes/instances and multi-file workflows are not atomic. A later database adapter should provide transactions and optimistic concurrency.
