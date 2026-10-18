# ADR 0006: E2E Tests Move Time Through a Development-Only Test Clock

## Status
Accepted

## Context
Freshness, Completion Outcomes and the Daily lock all depend on "now" in `Europe/Warsaw`. The API reads time only through an injected `TimeProvider`. E2E tests used to fake the past by writing back-dated rows straight into MySQL: a `LastCompletedAt` with no Completion behind it, and `UrgencyAtCompletion` typed by hand. That skipped the Freshness math (ADR 0005), so the Completion History test was checking the UI against the test's own arithmetic. We considered (issue #40):
1. **A settable server clock behind a test-only endpoint.**
2. **A per-request time header**, honoured the way `TestAuthHandler` honours `X-Test-Sub`. It is safe under parallel runs, but adds a request-scoped `TimeProvider` adapter.
3. **Keep SQL seeding** and test time-dependent outcomes only in the endpoint tests.

## Decision
1. In the `Development` environment, `TimeProvider` resolves to `TestClock`. It is real time plus a settable offset, so time keeps moving after it is set. Every other environment, `Testing` included, gets `TimeProvider.System`.
2. `PUT /api/test/clock { utcNow }` and `DELETE /api/test/clock` are mapped only in `Development`. In any other environment the routes don't exist.
3. The endpoints don't require `JwtAuth:BypassAuth`. Being Development-only is the gate.
4. E2E seeds history through the regular API: move the clock, create or complete, and reset the clock before the browser loads. Completion Outcomes come from the domain. Member wallet balances are still set via SQL, because no API sets a balance.

## Consequences
- The clock is process-wide state, so E2E has to keep `workers: 1`. Moving to parallel workers means revisiting option 2.
- Anyone running the API locally in `Development` can move its clock. It is reset on restart and with `DELETE`.
- Seeded data is now created by the domain, so a change to the Freshness math shows up in the E2E history assertions.
