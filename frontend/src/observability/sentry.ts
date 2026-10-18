import * as Sentry from '@sentry/react'
import type { SentryConfig } from '../types/config'

export function initSentry(config: SentryConfig) {
  if (!config.dsn) return

  Sentry.init({
    dsn: config.dsn,
    environment: config.environment ?? undefined,
    release: config.release ?? undefined,
    integrations: [Sentry.browserTracingIntegration()],
    tracesSampleRate: config.tracesSampleRate ?? undefined,
  })
}

export function setSentryUser(sub: string | undefined) {
  Sentry.setUser(sub ? { id: sub } : null)
}
