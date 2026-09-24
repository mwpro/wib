import React, { useCallback, useEffect, useMemo, useState } from 'react'
import { Auth0Provider, useAuth0 } from '@auth0/auth0-react'
import type { ClientConfig } from '../types/config'
import { AuthContext, type AuthContextValue, type AuthUser } from './context'

const DEFAULT_TEST_USER: AuthUser = {
  sub: 'auth0|test-user-1',
  name: 'Test Użytkownik',
}

function MockAuthProvider({ children }: { children: React.ReactNode }) {
  const [testUser, setTestUser] = useState<AuthUser>(() => {
    const saved = localStorage.getItem('wib_test_user')
    if (saved) {
      try {
        return JSON.parse(saved)
      } catch {
        // ignore JSON parse error
      }
    }
    return DEFAULT_TEST_USER
  })

  const handleSetTestUser = useCallback((user: AuthUser) => {
    setTestUser(user)
    localStorage.setItem('wib_test_user', JSON.stringify(user))
  }, [])

  const value: AuthContextValue = useMemo(() => ({
    isAuthenticated: true,
    isLoading: false,
    user: testUser,
    isTestMode: true,
    login: () => {},
    logout: () => {},
    getAccessToken: async () => 'test-bearer-token',
    setTestUser: handleSetTestUser,
  }), [testUser, handleSetTestUser])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

function Auth0Bridge({
  children,
  audience,
}: {
  children: React.ReactNode
  audience?: string
}) {
  const {
    isAuthenticated,
    isLoading,
    user,
    loginWithRedirect,
    logout,
    getAccessTokenSilently,
  } = useAuth0()

  const authUser: AuthUser | undefined = user
    ? {
        sub: user.sub || '',
        name: user.name || user.nickname || 'Użytkownik',
      }
    : undefined

  const value: AuthContextValue = useMemo(() => ({
    isAuthenticated,
    isLoading,
    user: authUser,
    isTestMode: false,
    login: () => loginWithRedirect(),
    logout: () =>
      logout({
        logoutParams: {
          returnTo: window.location.origin,
        },
      }),
    getAccessToken: async () => {
      try {
        const token = await getAccessTokenSilently({
          authorizationParams: audience ? { audience } : undefined,
        })
        return token ?? null
      } catch {
        return null
      }
    },
  }), [isAuthenticated, isLoading, authUser?.sub, authUser?.name, audience, loginWithRedirect, logout, getAccessTokenSilently])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [config, setConfig] = useState<ClientConfig | null>(null)
  const [loadingConfig, setLoadingConfig] = useState(true)
  const [configError, setConfigError] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/config')
      .then((res) => {
        if (!res.ok) {
          throw new Error(`Failed to load config: ${res.statusText}`)
        }
        return res.json()
      })
      .then((data: ClientConfig) => {
        setConfig(data)
        setLoadingConfig(false)
      })
      .catch((err) => {
        console.error('Error loading config:', err)
        setConfigError(err.message)
        setLoadingConfig(false)
      })
  }, [])

  if (loadingConfig) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400">
        <div className="flex flex-col items-center gap-3">
          <div className="w-8 h-8 border-4 border-amber-500 border-t-transparent rounded-full animate-spin" />
          <p className="text-sm font-medium">Ładowanie konfiguracji wib...</p>
        </div>
      </div>
    )
  }

  if (configError || !config) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 text-slate-800 dark:text-slate-200 p-4">
        <div className="max-w-md w-full bg-white dark:bg-slate-900 border border-red-200 dark:border-red-900 rounded-2xl p-6 shadow-sm">
          <h2 className="text-lg font-bold text-red-600 dark:text-red-400 mb-2">Błąd połączenia z serwerem</h2>
          <p className="text-sm text-slate-600 dark:text-slate-400 mb-4">
            Nie udało się pobrać konfiguracji aplikacji z serwera. Upewnij się, że backend jest uruchomiony.
          </p>
          <button
            onClick={() => window.location.reload()}
            className="w-full py-2 px-4 bg-amber-500 hover:bg-amber-600 text-white font-medium rounded-xl transition"
          >
            Spróbuj ponownie
          </button>
        </div>
      </div>
    )
  }

  if (config.isTestMode) {
    return <MockAuthProvider>{children}</MockAuthProvider>
  }

  const jwtAuth = config.jwtAuth
  const domain = jwtAuth?.domain || (jwtAuth?.authority ? jwtAuth.authority.replace(/^https?:\/\//, '').replace(/\/$/, '') : '')
  const clientId = jwtAuth?.clientId || ''
  const audience = jwtAuth?.audience || undefined

  if (!domain || !clientId) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 p-4">
        <div className="max-w-md w-full bg-white dark:bg-slate-900 border border-amber-300 dark:border-amber-800 rounded-2xl p-6 shadow-sm">
          <h2 className="text-lg font-bold text-amber-600 mb-2">Brak konfiguracji logowania (JwtAuth)</h2>
          <p className="text-sm text-slate-600 dark:text-slate-400">
            Aplikacja działa w trybie produkcyjnym, lecz parametry logowania (JwtAuth: Authority/Domain, ClientId) nie zostały skonfigurowane w backendzie.
          </p>
        </div>
      </div>
    )
  }

  return (
    <Auth0Provider
      domain={domain}
      clientId={clientId}
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience,
      }}
    >
      <Auth0Bridge audience={audience}>{children}</Auth0Bridge>
    </Auth0Provider>
  )
}

