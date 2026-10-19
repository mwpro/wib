import { request } from './client'
import type { AuthContextValue } from '../auth/context'
import type {
  ChoreHistoryResponse,
  ChoreHistorySummary,
  ChoreResponse,
  CompleteChoreResponse,
  CreateChoreRequest,
  UpdateChoreRequest,
} from '../types/chore'

export const fetchChores = (auth: AuthContextValue, tag?: string) =>
  request<ChoreResponse[]>(auth, '/chores', {
    query: { tag },
    failureMessage: 'Nie udało się pobrać zadań.',
  })

export const completeChoreApi = (auth: AuthContextValue, choreId: number) =>
  request<CompleteChoreResponse>(auth, `/chores/${choreId}/completion`, {
    method: 'POST',
    failureMessage: 'Nie udało się ukończyć zadania.',
  })

export const createChoreApi = (auth: AuthContextValue, req: CreateChoreRequest) =>
  request<ChoreResponse>(auth, '/chores', {
    method: 'POST',
    body: req,
    failureMessage: 'Nie udało się utworzyć zadania.',
  })

export const updateChoreApi = (auth: AuthContextValue, choreId: number, req: UpdateChoreRequest) =>
  request<ChoreResponse>(auth, `/chores/${choreId}`, {
    method: 'PUT',
    body: req,
    failureMessage: 'Nie udało się zaktualizować zadania.',
  })

export const deleteChoreApi = (auth: AuthContextValue, choreId: number) =>
  request<void>(auth, `/chores/${choreId}`, {
    method: 'DELETE',
    failureMessage: 'Nie udało się usunąć zadania.',
  })

export const fetchChoreHistory = (auth: AuthContextValue, choreId: number, page: number = 1) =>
  request<ChoreHistoryResponse>(auth, `/chores/${choreId}/completions`, {
    query: { page },
    failureMessage: 'Nie udało się pobrać historii zadania.',
  })

export const fetchChoreHistorySummary = (auth: AuthContextValue, choreId: number) =>
  request<ChoreHistorySummary>(auth, `/chores/${choreId}/completions/summary`, {
    failureMessage: 'Nie udało się pobrać podsumowania zadania.',
  })
