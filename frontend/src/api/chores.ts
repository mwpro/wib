import { fetchWithAuth } from './client'
import type { AuthContextValue } from '../auth/context'
import type {
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
    throw new Error(`Nie udało się ukończyć zadania: ${response.statusText}`)
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
    const errorBody = await response.text()
    throw new Error(`Nie udało się utworzyć zadania: ${errorBody || response.statusText}`)
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
    const errorBody = await response.text()
    throw new Error(`Nie udało się zaktualizować zadania: ${errorBody || response.statusText}`)
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
    throw new Error(`Nie udało się usunąć zadania: ${response.statusText}`)
  }
}
