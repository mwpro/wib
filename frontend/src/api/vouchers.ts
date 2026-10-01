import { fetchWithAuth } from './client'
import type { AuthContextValue } from '../auth/context'
import type { VoucherResponse } from '../types/voucher'

export async function fetchVouchers(
  auth: AuthContextValue,
  isRedeemed?: boolean
): Promise<VoucherResponse[]> {
  const url =
    isRedeemed !== undefined
      ? `/api/vouchers?isRedeemed=${isRedeemed}`
      : '/api/vouchers'

  const response = await fetchWithAuth(url, auth)
  if (!response.ok) {
    throw new Error(`Nie udało się pobrać kuponów: ${response.statusText}`)
  }
  return response.json()
}

export async function redeemVoucherApi(
  auth: AuthContextValue,
  id: number
): Promise<VoucherResponse> {
  const response = await fetchWithAuth(`/api/vouchers/${id}/redemption`, auth, {
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
    throw new Error(detail || 'Nie udało się zrealizować kuponu.')
  }
  return response.json()
}
