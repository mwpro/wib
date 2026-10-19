# ADR 0001: Architecture, Stack, and Deployment Model

## Status
Accepted (Amended by [ADR 0003](0003-domain-project-extraction.md) and [ADR 0007](0007-domain-rule-outcomes-as-results.md))

## Context
We need a responsive, low-maintenance, resource-efficient platform for **wib** ("Who is Better"), hosted on a lightweight Mikr.us VPS (Linux) behind an existing Nginx reverse proxy. The primary daily interaction is via mobile phones (iOS Safari).

## Decision
1. **Backend**: ASP.NET Core (.NET 10) REST API in C# with domain extracted to a dedicated `Wib.Domain` class library (see [ADR 0003](0003-domain-project-extraction.md)).
2. **Database & ORM**: Entity Framework Core on the MariaDB database hosted on Mikr.us. Schema migrations run automatically at application startup.
3. **Frontend**: React SPA in TypeScript, built with Vite. Server state is cached client-side rather than held in a global store.
4. **Localization**: UI displayed in Polish; code, domain entities, and API contracts in English.
   - *Amended 2026-10-19*: the API no longer produces Polish error text. Rule violations reach the SPA as English error codes, and the SPA translates them (see [ADR 0007](0007-domain-rule-outcomes-as-results.md)).
5. **Authentication**: Auth0 JWT Bearer authentication. Just-In-Time (JIT) provisioning registers new `Member` records on first authenticated request. The API serves client configuration via `GET /api/config`.
6. **Deployment**: one container built by a multi-stage Dockerfile, with the API serving the built SPA from `wwwroot`. Orchestrated via Docker Compose, with secrets mounted from `/run/secrets/appsettings.secret.json`.
7. **Automated Testing Strategy (Optimized for Agent Autonomy)**:
   - **Playwright E2E**: a few lean golden-path journeys (Chore loop, Scoreboard loop, Store/Wallet loop) through the full stack (browser + API + MySQL).
   - **Test Authentication Bypass**: `TestAuthHandler`, enabled only by `JwtAuth:BypassAuth=true`, lets tests authenticate as synthetic members without contacting Auth0.
   - **Backend tests (`Wib.UnitTests`)**: xUnit + FluentAssertions. Domain rules are tested directly on `Wib.Domain`. HTTP concerns (auth, error mapping, persistence, rules that need persisted ids) are tested in-process through the HTTP pipeline.
   - **Frontend**: no unit or component tests. The SPA relies on strict TypeScript, linting, and Playwright.

Current tool versions and commands live in [AGENTS.md](../../AGENTS.md), not here.

## Consequences
- Single container deployment minimizes RAM and CPU footprint on the Mikr.us VPS.
- Mirrors the proven deployment architecture of `tiny-money`.
- Automated testing provides a fast, reliable regression suite for human and AI development.
- Error monitoring was added later as its own decision: [ADR 0008](0008-sentry-error-monitoring.md).
