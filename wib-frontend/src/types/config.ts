export interface JwtAuthConfig {
  authority: string
  domain?: string
  clientId: string
  audience: string
}

export interface ClientConfig {
  jwtAuth?: JwtAuthConfig
  isTestMode: boolean
}
