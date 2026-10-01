import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { fetchVouchers, redeemVoucherApi } from '../api/vouchers'
import type { VoucherResponse } from '../types/voucher'

export function useActiveVouchers() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<VoucherResponse[]>({
    queryKey: ['vouchers', { isRedeemed: false }],
    queryFn: () => fetchVouchers(auth, false),
    enabled: isAuthenticated,
  })
}

export function useRedeemedVouchers() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<VoucherResponse[]>({
    queryKey: ['vouchers', { isRedeemed: true }],
    queryFn: () => fetchVouchers(auth, true),
    enabled: isAuthenticated,
  })
}

export function useRedeemVoucher() {
  const auth = useAuth()
  const queryClient = useQueryClient()

  return useMutation<VoucherResponse, Error, number>({
    mutationFn: (voucherId) => redeemVoucherApi(auth, voucherId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vouchers'] })
    },
  })
}
