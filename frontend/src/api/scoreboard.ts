import { fetchWithAuth } from './client'
import type { AuthContextValue } from '../auth/context'
import type { ScoreboardResponse } from '../types/scoreboard'

export async function fetchScoreboard(
  auth: AuthContextValue,
  year: number,
  month: number
): Promise<ScoreboardResponse> {
  const response = await fetchWithAuth(`/api/scoreboard/${year}/${month}`, auth)
  if (!response.ok) {
    throw new Error(`Nie udało się pobrać tabeli wyników: ${response.statusText}`)
  }
  return response.json()
}
