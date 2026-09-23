import type { Member } from '../types/member'
import type { AuthContextValue } from '../auth/context'

export async function fetchWithAuth(
  url: string,
  auth: AuthContextValue,
  init?: RequestInit
): Promise<Response> {
  const headers = new Headers(init?.headers)

  if (auth.isTestMode && auth.user) {
    headers.set('X-Test-Sub', auth.user.sub)
    headers.set('X-Test-User-Name', auth.user.name)
    if (auth.user.email) headers.set('X-Test-User-Email', auth.user.email)
    if (auth.user.picture) headers.set('X-Test-User-Picture', auth.user.picture)
  } else {
    const token = await auth.getAccessToken()
    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }
  }

  return fetch(url, {
    ...init,
    headers,
  })
}

export async function getCurrentMember(auth: AuthContextValue): Promise<Member | null> {
  const response = await fetchWithAuth('/api/members/me', auth)
  if (!response.ok) {
    if (response.status === 401) {
      return null
    }
    throw new Error(`Failed to fetch current member: ${response.statusText}`)
  }
  return response.json()
}
