# wib TRMNL Display

Private TRMNL polling plugin (quadrant layout only) for the **TRMNL Display**. What it shows is defined in [CONTEXT.md](../CONTEXT.md). TRMNL authenticates to the wib API as an **Auth0 OAuth client**. See [ADR 0004](../docs/adr/0004-trmnl-display-via-oauth.md) for the reasoning.

```
trmnl/
├── .trmnlp.yml          # local preview config + sample data (IDX_0 = chores, IDX_1 = scoreboard)
└── src/
    ├── settings.yml     # polling URLs, Bearer header, OAuth config (no secrets), custom fields, 15 min refresh
    └── quadrant.liquid  # the only layout
```

`trmnlp pull` writes the non-secret OAuth config (`oauth_*` keys: URLs, scopes, audience param, Auth0 subdomain) into `settings.yml`, so it's versioned. The **client ID/secret** stay in the TRMNL UI only. Never commit them.

## One-time setup

### 1. Auth0: create the TRMNL application
1. In the Auth0 tenant (the one shared with tiny-money), go to **Applications → Create Application → Regular Web Application**, named `TRMNL`.
2. Under **Settings → Advanced → Grant Types**, enable **Authorization Code** and **Refresh Token** only.
3. Under **Settings → Refresh Token Rotation**, turn rotation **off**. Under **Refresh Token Expiration**, set a long **absolute** lifetime (e.g. 31 536 000 s = 1 year) and disable inactivity expiration. When it expires, reconnect (step 2.4).
4. Make sure the wib API (`https://prod.wib`) has **Allow Offline Access** enabled under **APIs → wib → Settings → Access Settings**. Without it, Auth0 won't issue refresh tokens.
5. Leave **Allowed Callback URLs** empty for now. You'll paste TRMNL's redirect URL there in step 2.3.
6. Under **APIs → wib → Application Access**, edit `TRMNL`: **User-delegated access = Authorized** (no permissions needed), **Client access** and **Anonymous access = Unauthorized**. The wib API requires an explicit grant for user tokens. Without it, Connect fails with `Client "…" is not authorized to access resource server "https://prod.wib"`.
7. In the app's **Connections** tab, enable the same login connection(s) the wib SPA uses. Otherwise login fails with "no connections enabled for the client".

### 2. TRMNL: create the private plugin
1. Push the plugin from this folder (see [Development](#development)). Alternatively, create a Private Plugin manually with strategy **Polling** and paste `src/settings.yml` and `src/quadrant.liquid`.
2. In the plugin settings, set **Instancja wib** to the prod base URL **with a trailing slash**.
3. Turn on **Enable OAuth** and fill in:
   | Field | Value |
   |---|---|
   | Authorization URL | `https://<tenant>.auth0.com/authorize` |
   | Token URL | `https://<tenant>.auth0.com/oauth/token` |
   | Client ID / Secret | from the Auth0 `TRMNL` app |
   | Scopes | `openid offline_access` (**required**, space-separated). If it's empty, Auth0 issues no refresh token and the widget dies when the first access token expires. |
   | Custom auth params | `audience=https://prod.wib` |
   | Token auth method | body (`client_secret_post`, Auth0's default for Regular Web Apps) |

   Copy the **OAuth Redirect URL** that TRMNL shows into the Auth0 app's **Allowed Callback URLs** and save.
4. Click **Connect** and log in as a household member. The token carries that member's identity and full API access, which is intentional (ADR 0004).
5. Add the plugin to a **2×2 mashup** in the device playlist.

### 3. If polling returns 401: audience fallback
If TRMNL's hosted OAuth UI has no custom auth params (or ignores them), Auth0 issues an opaque token and wib rejects it. Fix it with the following, then reconnect (step 2.4):

- Auth0 **Settings → General → API Authorization Settings → Default Audience** = `https://prod.wib`.

This is tenant-wide. wib and tiny-money SPAs both send their own `audience`, so they're unaffected, but **any future client in this tenant that omits `audience` will get wib tokens**.

If that still fails, OAuth is a dead end: supersede ADR 0004 with a tiny-money-style API key.

## Development

Preview locally with [trmnlp](https://github.com/usetrmnl/trmnlp), here via Docker:

```powershell
# Render all layouts to trmnl/_build/ using the sample data in .trmnlp.yml
docker run --rm -v "${PWD}\trmnl:/plugin" trmnl/trmnlp build

# Live preview server (http://localhost:4567)
docker run --rm -p 4567:4567 -v "${PWD}\trmnl:/plugin" trmnl/trmnlp serve

# Push to TRMNL (needs `trmnlp login` / TRMNL_API_KEY)
docker run --rm -it -v "${PWD}\trmnl:/plugin" -e TRMNL_API_KEY trmnl/trmnlp push
```

trmnlp tries to poll `wib_url` and warns that TRMNL refuses private addresses. The template then renders from `variables` in `.trmnlp.yml`. When you change the API response shape, update that sample data.

### Known quirks
- **Month boundary**: the scoreboard URL uses Liquid's `now`, which may be UTC. For an hour or two after midnight on the 1st, the race can show the previous month. This is accepted (ADR 0004).
- **Array responses**: the template reads `IDX_0.data | default: IDX_0`, because TRMNL may wrap a root-level JSON array in `data`.
