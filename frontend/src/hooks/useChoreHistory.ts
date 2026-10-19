import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { fetchChoreHistory, fetchChoreHistorySummary } from '../api/chores'
import { queryKeys } from './queryCache'
import type { ChoreHistoryResponse, ChoreHistorySummary } from '../types/chore'

export function useChoreHistory(choreId: number | null) {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useInfiniteQuery<ChoreHistoryResponse>({
    queryKey: queryKeys.choreHistoryItems(choreId),
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
    queryKey: queryKeys.choreHistorySummary(choreId),
    queryFn: () => fetchChoreHistorySummary(auth, choreId!),
    enabled: isAuthenticated && choreId != null,
  })
}
