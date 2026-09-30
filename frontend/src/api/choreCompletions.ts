import { fetchWithAuth } from './client'
import type { AuthContextValue } from '../auth/context'
import type { ChoreCompletionsResponse } from '../types/choreCompletion'

export async function fetchChoreCompletions(
  auth: AuthContextValue,
  year: number,
  month: number,
  page: number = 1,
  pageSize: number = 10
): Promise<ChoreCompletionsResponse> {
  const response = await fetchWithAuth(
    `/api/chore-completions/${year}/${month}?page=${page}&pageSize=${pageSize}`,
    auth
  )
  if (!response.ok) {
    throw new Error(`Nie udało się pobrać aktywności zadań: ${response.statusText}`)
  }
  return response.json()
}
