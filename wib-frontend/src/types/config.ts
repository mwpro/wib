export interface Auth0Config {
  domain: string
  clientId: string
  audience: string
}

export interface ClientConfig {
  auth0: Auth0Config
  isTestMode: boolean
}
