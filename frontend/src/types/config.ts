export interface JwtAuthConfig {
  authority: string
  domain?: string
  clientId: string
  audience: string
}

export interface ClientConfig {
  jwtAuth?: JwtAuthConfig
  isTestMode: boolean
  sentry?: SentryConfig
}

export interface SentryConfig {
  dsn?: string | null
  environment?: string | null
  release?: string | null
}
