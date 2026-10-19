# ADR 0008: Error Monitoring with Sentry

## Status
Accepted (2026-10-18; originally recorded as §8 of [ADR 0001](0001-architecture-and-stack.md))

## Context
ADR 0001 shipped without error monitoring, so failures on the backend or in a member's browser left no trace outside the container logs. Household traffic is small, which keeps a hosted tool within its free tier and adds nothing to run on the Mikr.us VPS.

## Decision
1. One Sentry project for backend (`Sentry.AspNetCore`) and SPA (`@sentry/react`), separated by environment (the ASP.NET hosting environment). A single DSN lives in `Sentry:Dsn` (`appsettings.secret.json` in production); the SPA receives DSN, environment and release from `GET /api/config`. An empty DSN disables both sides, so local runs, CI and Playwright (which forces `Sentry__Dsn=''`) report nothing.
2. Release is the git commit SHA, passed by CI as the `GIT_SHA` Docker build argument, so both sides share it.
3. Backend: issues from `Error` level only (warnings stay breadcrumbs), no log forwarding, `SendDefaultPii` off; events carry the Auth0 `sub` as the user id and nothing else. Tracing at 100% — household traffic is far below the free tier.
4. SPA: `Sentry.ErrorBoundary` wraps the whole app (outside auth and query providers) with a reload screen; the user is set to the Auth0 `sub`. Query errors the UI already handles are not reported; the backend reports server-side failures.
5. Deliberately skipped: source map upload or publishing (frontend stack traces stay minified), session replay, and an ad-blocker tunnel endpoint.

## Consequences
- Domain rule violations are not errors and never reach Sentry; only bugs and infrastructure failures do (see [ADR 0007](0007-domain-rule-outcomes-as-results.md)).
- Frontend stack traces are minified, which makes SPA issues harder to read.
- Members using ad blockers may not report SPA errors.
