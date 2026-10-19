import { fetchWithAuth } from './client'
import { apiErrorMessage } from './errors'
import type { AuthContextValue } from '../auth/context'
import type {
  ChoreHistoryResponse,
  ChoreHistorySummary,
  ChoreResponse,
  CompleteChoreResponse,
  CreateChoreRequest,
  UpdateChoreRequest,
} from '../types/chore'

export async function fetchChores(
  auth: AuthContextValue,
  tag?: string
): Promise<ChoreResponse[]> {
  const url = tag ? `/api/chores?tag=${encodeURIComponent(tag)}` : '/api/chores'
  const response = await fetchWithAuth(url, auth)
  if (!response.ok) {
    throw new Error(`Nie udało się pobrać zadań: ${response.statusText}`)
  }
  return response.json()
}

export async function completeChoreApi(
  auth: AuthContextValue,
  choreId: number
): Promise<CompleteChoreResponse> {
  const response = await fetchWithAuth(`/api/chores/${choreId}/completion`, auth, {
    method: 'POST',
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się ukończyć zadania.'))
  }
  return response.json()
}

export async function createChoreApi(
  auth: AuthContextValue,
  req: CreateChoreRequest
): Promise<ChoreResponse> {
  const response = await fetchWithAuth('/api/chores', auth, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(req),
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się utworzyć zadania.'))
  }
  return response.json()
}

export async function updateChoreApi(
  auth: AuthContextValue,
  choreId: number,
  req: UpdateChoreRequest
): Promise<ChoreResponse> {
  const response = await fetchWithAuth(`/api/chores/${choreId}`, auth, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(req),
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się zaktualizować zadania.'))
  }
  return response.json()
}

export async function deleteChoreApi(
  auth: AuthContextValue,
  choreId: number
): Promise<void> {
  const response = await fetchWithAuth(`/api/chores/${choreId}`, auth, {
    method: 'DELETE',
  })
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się usunąć zadania.'))
  }
}

export async function fetchChoreHistory(
  auth: AuthContextValue,
  choreId: number,
  page: number = 1
): Promise<ChoreHistoryResponse> {
  const response = await fetchWithAuth(
    `/api/chores/${choreId}/completions?page=${page}`,
    auth
  )
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się pobrać historii zadania.'))
  }
  return response.json()
}

export async function fetchChoreHistorySummary(
  auth: AuthContextValue,
  choreId: number
): Promise<ChoreHistorySummary> {
  const response = await fetchWithAuth(`/api/chores/${choreId}/completions/summary`, auth)
  if (!response.ok) {
    throw new Error(await apiErrorMessage(response, 'Nie udało się pobrać podsumowania zadania.'))
  }
  return response.json()
}
