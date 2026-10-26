# ADR-003: Mixed Basket Uses Composition

- **Status:** Accepted
- **Context:** A counter sale can combine retail products with shipping and money-order services.
- **Decision:** `CounterTransaction` owns a list of `TransactionLine` records tagged by `LineKind`. Service-specific details remain in `Shipment` and `MoneyOrder` records linked to the transaction rather than forcing every domain concept into a deep inheritance hierarchy.
- **Consequences:** Totals and lifecycle rules are centralized, and new line kinds can be added deliberately. Cross-file persistence cannot guarantee full atomicity in JSON; the limitation is explicit and a database transaction is the intended future solution.
