# ADR-001: Layered Architecture with Pragmatic DDD

- **Status:** Accepted
- **Context:** Assessment 02 changes the Week 1 resource-allocation domain into branch retail, shipping, money orders, identity verification and mixed counter transactions. Rules now protect inventory and financial workflows.
- **Decision:** Use Domain, Application, Infrastructure and Web projects. Apply DDD entities, value objects, transaction aggregate behavior, domain policies and events only where they enforce meaningful invariants. Keep Domain independent of MVC, JSON and hosting.
- **Consequences:** Business rules can be tested without a web host or filesystem. More files exist than in the original console app, but responsibilities are explicit. Do not add microservices, CQRS or event sourcing for this single-process lab.
