# ADR-005: Cookie Authentication Without ASP.NET Core Identity

- **Status:** Accepted
- **Context:** The assignment explicitly prohibits ASP.NET Core Identity in Week 2 and requires a login page, claims, roles, branch policies and secure cookies.
- **Decision:** Use ASP.NET Core cookie authentication with claims `StaffId`, role and `BranchCode`. Store only PBKDF2-SHA256 salted password hashes. Configure HttpOnly, Secure, SameSite=Lax, expiration and antiforgery. Provision demo passwords through user-secrets, never source code.
- **Consequences:** The implementation matches the lab and avoids external identity packages. Password reset, lockout persistence, MFA, production account provisioning and identity-provider integration are not claimed; they require explicit future design.
