# ADR-007: MVC Action Audit with Append-Only JSON Lines

- **Status:** Accepted
- **Context:** Important MVC actions need actor, role, branch, action, status, duration, time and correlation ID. Sensitive data must not enter audit logs.
- **Decision:** Use a global `IAsyncActionFilter` because it has MVC controller/action/route/result context. Write sanitized `AuditRecord` objects through `IAuditStore` to one JSON object per line in append mode. Route values are allowlisted; request bodies are never serialized.
- **Consequences:** Records are naturally append-only at the file-write level and easy to inspect. The file needs retention/backup controls and can grow indefinitely; JSONL is not tamper-evident against a privileged filesystem user. Production needs protected centralized audit storage.
