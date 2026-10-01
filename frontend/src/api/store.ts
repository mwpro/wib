import { fetchWithAuth } from './client'
import type { AuthContextValue } from '../auth/context'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
  BuyRewardResponse,
} from '../types/store'

export async function fetchStoreItems(
  auth: AuthContextValue
): Promise<RewardItemResponse[]> {
  const response = await fetchWithAuth('/api/store/items', auth)
  if (!response.ok) {
    throw new Error(`Nie udało się pobrać listy nagród: ${response.statusText}`)
  }
  return response.json()
}

export async function createStoreItemApi(
  auth: AuthContextValue,
  req: CreateRewardItemRequest
): Promise<RewardItemResponse> {
  const response = await fetchWithAuth('/api/store/items', auth, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(req),
  })
  if (!response.ok) {
    const errorBody = await response.text()
    throw new Error(`Nie udało się utworzyć nagrody: ${errorBody || response.statusText}`)
  }
  return response.json()
}

export async function updateStoreItemApi(
  auth: AuthContextValue,
  id: number,
  req: UpdateRewardItemRequest
): Promise<RewardItemResponse> {
  const response = await fetchWithAuth(`/api/store/items/${id}`, auth, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(req),
  })
  if (!response.ok) {
    const errorBody = await response.text()
    throw new Error(`Nie udało się zaktualizować nagrody: ${errorBody || response.statusText}`)
  }
  return response.json()
}

export async function deleteStoreItemApi(
  auth: AuthContextValue,
  id: number
): Promise<void> {
  const response = await fetchWithAuth(`/api/store/items/${id}`, auth, {
    method: 'DELETE',
  })
  if (!response.ok) {
    throw new Error(`Nie udało się usunąć nagrody ze sklepu: ${response.statusText}`)
  }
}

export async function purchaseStoreItemApi(
  auth: AuthContextValue,
  id: number
): Promise<BuyRewardResponse> {
  const response = await fetchWithAuth(`/api/store/items/${id}/purchase`, auth, {
    method: 'POST',
  })
  if (!response.ok) {
    let detail = response.statusText
    try {
      const errJson = await response.json()
      if (errJson.detail) detail = errJson.detail
      else if (errJson.title) detail = errJson.title
    } catch {
      // ignore
    }
    throw new Error(detail || 'Nie udało się kupić nagrody.')
  }
  return response.json()
}
