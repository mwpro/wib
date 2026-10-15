# ADR 0004: TRMNL Display via OAuth (TRMNL as Auth0 Client)

## Status
Accepted (experimental: relies on TRMNL's beta "Enable OAuth" support for private plugins)

## Context
We want a **TRMNL Display**: a read-only, household-wide view of wib on the home's TRMNL X e-ink device (TRMNL cloud, Developer Edition). It shows the top of the backlog and the Monthly Race.

The previous TRMNL integration (`tiny-money`) used a homegrown API key feature: key table, hashing, settings UI, custom `ApiKey` auth handler. We want to avoid building and maintaining that again.

TRMNL offers three relevant mechanisms:
1. **Private plugin, polling, "Enable OAuth"**: TRMNL acts as an OAuth 2 client against a third-party provider. It runs the authorization-code flow once, stores the refresh token, refreshes on its own (retrying once on `401`), and exposes `{{ oauth_access_token }}` to polling headers.
2. **Public plugin (TRMNL as OAuth provider)**: install/uninstall webhooks, markup endpoint and management URL. Far too much plumbing for a single household.
3. **Webhook push**: no inbound auth, but needs a background push job and is limited to 12 requests/hour and 5 KB payloads.

## Decision
1. **Private polling plugin** with source in [`trmnl/`](../../trmnl/), managed with `trmnlp`. **Quadrant layout only.** Refreshes every 15 minutes.
2. **Regular API endpoints only**. No TRMNL-specific endpoint and no backend changes:
   - `GET /api/chores`: backlog order is already applied server-side. The template shows up to 10 **scheduled** chores in backlog order: Neglected, Overdue and Due Soon first, then Fresh ones fill the remaining rows. Unscheduled chores are never shown.
   - `GET /api/scoreboard/{year}/{month}`: year and month come from Liquid's `now` in the polling URL. **Accepted defect**: around the start of a month the date may resolve in UTC and show the previous month's race for an hour or two.
3. **Auth: TRMNL as an Auth0 client** of the existing tenant (shared with `tiny-money`):
   - Dedicated Auth0 **Regular Web Application** "TRMNL" with `authorization_code` + `refresh_token` grants and the `offline_access` scope.
   - **Refresh-token rotation off**, with a long absolute lifetime. A rotated token that TRMNL failed to persist would trigger reuse detection and revoke the family, which is a real risk with a beta client. When the lifetime runs out, reconnect manually.
   - Audience resolution, in order of preference:
     1. Pass `audience` as a custom authorize parameter in TRMNL's OAuth config.
     2. If the hosted UI can't do that, set the tenant **Default Audience** to the wib API. This is safe today because both SPAs (wib and tiny-money) send their audience explicitly. Any future client in the tenant that omits `audience` will silently receive wib tokens.
     3. If neither works, fall back to the API-key approach (see Consequences).
4. **The TRMNL token intentionally has full member access**: it carries the `sub` of whoever connected it. We deliberately skip `azp`/scope restrictions and leave JIT member provisioning untouched. The device is in the home, the plugin is private, and the data is household-wide anyway. The accepted risk is that a leaked TRMNL token equals that member's account.
5. **The non-secret OAuth config is versioned; the secrets are not.** `trmnlp pull` writes the `oauth_*` keys (URLs, scopes, audience param, Auth0 subdomain) into `settings.yml`. The client ID/secret stay in the TRMNL UI only and are never committed. The wib base URL is a plugin custom field.

## Consequences
- No API key storage, hashing, rotation UI or custom auth handler in wib. Standard JWT Bearer validation covers TRMNL.
- Depends on a beta TRMNL feature. **Fallback plan**: if hosted OAuth refresh proves flaky or the audience can't be set, supersede this ADR with an API-key scheme modelled on `tiny-money`.
- Polling unfiltered `/api/chores` transfers the full backlog (descriptions included) every 15 minutes. That's acceptable at household size, but TRMNL rejects merge data around 100 KB.
- The Display is not exercised by Playwright. It is verified with `trmnlp` local preview against sample data. Setup steps live in [`trmnl/README.md`](../../trmnl/README.md).
