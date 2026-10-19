import { useMutation, useQuery } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import { fetchVouchers, redeemVoucherApi } from '../api/vouchers'
import { queryKeys, useQueryCache } from './queryCache'
import type { VoucherResponse } from '../types/voucher'

export function useActiveVouchers() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<VoucherResponse[]>({
    queryKey: queryKeys.vouchers(false),
    queryFn: () => fetchVouchers(auth, false),
    enabled: isAuthenticated,
  })
}

export function useRedeemedVouchers() {
  const auth = useAuth()
  const { isAuthenticated } = auth

  return useQuery<VoucherResponse[]>({
    queryKey: queryKeys.vouchers(true),
    queryFn: () => fetchVouchers(auth, true),
    enabled: isAuthenticated,
  })
}

export function useRedeemVoucher() {
  const auth = useAuth()
  const cache = useQueryCache()

  return useMutation<VoucherResponse, Error, number>({
    mutationFn: (voucherId) => redeemVoucherApi(auth, voucherId),
    onSuccess: () => cache.voucherRedeemed(),
  })
}
