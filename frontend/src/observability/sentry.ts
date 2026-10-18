import * as Sentry from '@sentry/react'
import type { SentryConfig } from '../types/config'

// Sentry stays off unless the backend hands out a DSN (empty locally, in CI and in e2e).
export function initSentry(config: SentryConfig | undefined) {
  if (!config?.dsn) return

  Sentry.init({
    dsn: config.dsn,
    environment: config.environment ?? undefined,
    release: config.release ?? undefined,
    integrations: [Sentry.browserTracingIntegration()],
    tracesSampleRate: 1.0,
  })
}

// Only the Auth0 subject is attached; no names or emails leave the browser.
export function setSentryUser(sub: string | undefined) {
  Sentry.setUser(sub ? { id: sub } : null)
}
