import { ApiError, request } from './client'
import type { AuthContextValue } from '../auth/context'
import type { Member } from '../types/member'

export async function getCurrentMember(auth: AuthContextValue): Promise<Member | null> {
  try {
    return await request<Member>(auth, '/members/me', {
      failureMessage: 'Nie udało się pobrać profilu domownika.',
    })
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) return null
    throw err
  }
}
