# ADR 0001: Architecture, Stack, and Deployment Model

## Status
Accepted

## Context
We need a responsive, low-maintenance, resource-efficient platform for **wib** ("Who is Better"), hosted on a lightweight Mikr.us VPS (Linux) behind an existing Nginx reverse proxy. The primary daily interaction is via mobile phones (iOS Safari).

## Decision
1. **Backend**: ASP.NET Core (.NET 10) REST API in C#.
2. **Database & ORM**: Entity Framework Core with `Pomelo.EntityFrameworkCore.MySql` connecting to the external MySQL database hosted on Mikr.us. Automatic schema migrations will run at application startup.
3. **Frontend**: React 19 SPA with TypeScript, built with Vite, styled with Tailwind CSS and Radix UI primitives. Uses React Query for server-state caching and React Hook Form for validations.
4. **Localization**: UI displayed in Polish; code, domain entities, and API contracts in English.
5. **Authentication**: Auth0 JWT Bearer authentication. Just-In-Time (JIT) provisioning registers new `Member` records on first authenticated request. The API serves client configuration via `GET /api/config`.
6. **Deployment**: Single multi-stage Dockerfile:
   - Stage 1: Build React SPA via `node:24-alpine`.
   - Stage 2: Publish ASP.NET Core API via `dotnet sdk:10.0`, copying SPA assets to `wwwroot`.
   - Stage 3: Runtime container using `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra`.
   - Orchestrated via Docker Compose with environment variables and secrets mounted from `/run/secrets/appsettings.secret.json`.
7. **Automated Testing Strategy (Optimized for Agent Autonomy)**:
   - **Playwright UI E2E Tests**: 3–4 lean golden-path journeys (Chore loop, Scoreboard loop, Store/Wallet loop) exercising the full stack (browser + API + MySQL) headlessly with screenshot/trace artifact generation on failure.
   - **Test Authentication Bypass**: An internal `TestAuthHandler` enabled exclusively in test environments (`Testing:BypassAuth=true`), allowing Playwright to authenticate instantly as synthetic members without contacting external Auth0 servers.
   - **Backend Unit Tests (`Wib.UnitTests`)**: Fast xUnit + FluentAssertions tests for algorithmic domain logic (freshness ratios, urgency thresholds, Europe/Warsaw timezone boundaries, voucher state rules).
   - **Frontend Verification**: Zero unit/component tests in Vitest. Relies on strict TypeScript compilation (`tsc --noEmit`), ESLint, and the Playwright E2E test suite.

## Consequences
- Single container deployment minimizes RAM and CPU footprint on the Mikr.us VPS.
- Mirrors the proven deployment architecture of `tiny-money`.
- Automated testing provides a fast, reliable regression suite for human and AI development.
