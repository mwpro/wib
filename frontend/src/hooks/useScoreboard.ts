import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { fetchScoreboard } from '../api/scoreboard'
import type { ScoreboardResponse } from '../types/scoreboard'

export function useScoreboard(year: number, month: number) {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<ScoreboardResponse>({
    queryKey: ['scoreboard', year, month],
    queryFn: () => fetchScoreboard(auth, year, month),
    enabled: isAuthenticated && year > 0 && month > 0,
  })
}
