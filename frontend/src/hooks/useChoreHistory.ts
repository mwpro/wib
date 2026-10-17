import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { fetchChoreHistory, fetchChoreHistorySummary } from '../api/chores'
import type { ChoreHistoryResponse, ChoreHistorySummary } from '../types/chore'

// Both keys share the ['chore-history', choreId] prefix, so one invalidation refreshes list and summary.
export function useChoreHistory(choreId: number | null) {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useInfiniteQuery<ChoreHistoryResponse>({
    queryKey: ['chore-history', choreId, 'items'],
    queryFn: ({ pageParam = 1 }) => fetchChoreHistory(auth, choreId!, pageParam as number),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => (lastPage.hasMore ? lastPage.page + 1 : undefined),
    enabled: isAuthenticated && choreId != null,
  })
}

export function useChoreHistorySummary(choreId: number | null) {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<ChoreHistorySummary>({
    queryKey: ['chore-history', choreId, 'summary'],
    queryFn: () => fetchChoreHistorySummary(auth, choreId!),
    enabled: isAuthenticated && choreId != null,
  })
}
