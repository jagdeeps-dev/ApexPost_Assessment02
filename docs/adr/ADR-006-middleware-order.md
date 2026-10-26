# ADR-006: Middleware Order and Endpoint Metadata

- **Status:** Accepted
- **Context:** Correlation, errors, headers, maintenance, CORS, rate limits, IP restrictions and API keys have ordering dependencies.
- **Decision:** Register correlation before exception handling; request timing around downstream processing; headers before response starts; routing before endpoint-metadata-dependent API key/IP middleware; CORS/rate limiting before endpoint execution; authentication before authorization. Mark integration endpoints with `RequireApiKeyAttribute` and `RestrictedEndpointAttribute`.
- **Consequences:** Middleware has clear responsibilities and tests can target HTTP behavior. Changing order requires a design review and regression tests. Forwarded headers are not trusted by default; deployment behind a proxy must configure trusted proxies explicitly.
