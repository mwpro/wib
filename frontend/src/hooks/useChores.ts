import { useMutation, useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import {
  completeChoreApi,
  createChoreApi,
  deleteChoreApi,
  fetchChores,
  updateChoreApi,
} from '../api/chores'
import { queryKeys, useQueryCache, type Rollback } from './queryCache'
import type {
  ChoreResponse,
  CompleteChoreResponse,
  CreateChoreRequest,
  UpdateChoreRequest,
} from '../types/chore'

export function useChores() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<ChoreResponse[]>({
    queryKey: queryKeys.chores(),
    queryFn: () => fetchChores(auth),
    enabled: isAuthenticated,
  })
}

export function useCompleteChore() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<CompleteChoreResponse, Error, { choreId: number; points: number }, Rollback>({
    mutationFn: ({ choreId }) => completeChoreApi(auth, choreId),
    onMutate: ({ choreId, points }) => cache.choreCompletionStarted(choreId, points),
    onError: (_err, _variables, rollback) => rollback?.(),
    onSuccess: (data) => cache.choreCompletionRecorded(data),
  })
}

export function useCreateChore() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<ChoreResponse, Error, CreateChoreRequest>({
    mutationFn: (req) => createChoreApi(auth, req),
    onSuccess: () => cache.choresChanged(),
  })
}

export function useUpdateChore() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<ChoreResponse, Error, { choreId: number; req: UpdateChoreRequest }>({
    mutationFn: ({ choreId, req }) => updateChoreApi(auth, choreId, req),
    onSuccess: () => cache.choresChanged(),
  })
}

export function useDeleteChore() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<void, Error, number>({
    mutationFn: (choreId) => deleteChoreApi(auth, choreId),
    onSuccess: () => cache.choresChanged(),
  })
}
