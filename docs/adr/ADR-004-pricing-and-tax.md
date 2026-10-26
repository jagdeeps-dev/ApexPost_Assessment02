# ADR-004: Shipping Strategy and Tax Policy

- **Status:** Accepted
- **Context:** Shipping rates vary by destination zone, shipment type, service level and maximum weight. Product tax categories differ.
- **Decision:** Use `IShippingPricingStrategy` with a zone-rate implementation. Keep tax mapping as an explicit pattern-matching policy. Use `decimal` and centralized two-decimal monetary rounding. A configurable `MoneyOrderPolicy` and `LowStockRule` encapsulate other changeable rules.
- **Consequences:** Pricing can be tested independently and alternate pricing strategies can be added without changing controller code. Avoid a generalized rules engine until there is a real requirement.
