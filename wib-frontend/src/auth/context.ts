import { createContext } from 'react'

export interface AuthUser {
  sub: string
  name: string
  email?: string
  picture?: string
}

export interface AuthContextValue {
  isAuthenticated: boolean
  isLoading: boolean
  user?: AuthUser
  isTestMode: boolean
  login: () => Promise<void> | void
  logout: () => Promise<void> | void
  getAccessToken: () => Promise<string | null>
  setTestUser?: (user: AuthUser) => void
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
