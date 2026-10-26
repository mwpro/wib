import { useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { fetchChoreReview } from '../api/scoreboard'
import { queryKeys } from './queryCache'
import type { ChoreReviewResponse } from '../types/scoreboard'

export function useChoreReview(year: number, month: number) {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<ChoreReviewResponse>({
    queryKey: queryKeys.choreReview(year, month),
    queryFn: () => fetchChoreReview(auth, year, month),
    enabled: isAuthenticated && year > 0 && month > 0,
  })
}
