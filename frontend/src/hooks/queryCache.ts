import { useQueryClient, type QueryClient, type QueryKey } from '@tanstack/react-query'
import { useAuth } from '../auth/useAuth'
import type { ChoreResponse, CompleteChoreResponse } from '../types/chore'
import type { Member } from '../types/member'
import type { BuyRewardResponse } from '../types/store'

export const queryKeys = {
  chores: () => ['chores'] as const,
  currentMember: (sub: string | undefined) => ['currentMember', sub] as const,
  storeItems: () => ['store', 'items'] as const,
  vouchers: (isRedeemed?: boolean) =>
    isRedeemed === undefined ? (['vouchers'] as const) : (['vouchers', { isRedeemed }] as const),
  scoreboard: (year?: number, month?: number) =>
    year === undefined ? (['scoreboard'] as const) : (['scoreboard', year, month] as const),
  choreReview: (year?: number, month?: number) =>
    year === undefined ? (['chore-review'] as const) : (['chore-review', year, month] as const),
  choreActivity: (year?: number, month?: number, pageSize?: number) =>
    year === undefined
      ? (['chore-activity'] as const)
      : (['chore-activity', year, month, pageSize] as const),
  choreHistory: (choreId: number | null) => ['chore-history', choreId] as const,
  choreHistoryItems: (choreId: number | null) => ['chore-history', choreId, 'items'] as const,
  choreHistorySummary: (choreId: number | null) => ['chore-history', choreId, 'summary'] as const,
}

export type Rollback = () => void

async function snapshot<T>(queryClient: QueryClient, key: QueryKey) {
  await queryClient.cancelQueries({ queryKey: key })
  const previous = queryClient.getQueryData<T>(key)
  return () => {
    if (previous !== undefined) queryClient.setQueryData(key, previous)
  }
}

function markFreshlyCompleted(chore: ChoreResponse): ChoreResponse {
  const isScheduled = chore.cadenceDays != null && chore.cadenceDays > 0
  return {
    ...chore,
    urgency: isScheduled ? 'Fresh' : 'Unscheduled',
    urgencyRatio: isScheduled ? 0 : null,
    daysSinceLastDone: 0,
    lastCompletedAt: new Date().toISOString(),
  }
}

export function useQueryCache() {
  const queryClient = useQueryClient()
  const { user } = useAuth()
  const memberKey = queryKeys.currentMember(user?.sub)

  const updateWallet = (balance: (current: number) => number) =>
    queryClient.setQueryData<Member | null>(memberKey, (old) =>
      old ? { ...old, walletBalance: balance(old.walletBalance) } : old
    )

  const invalidate = (...keys: QueryKey[]) =>
    keys.forEach((queryKey) => queryClient.invalidateQueries({ queryKey }))

  return {
    async choreCompletionStarted(choreId: number, points: number): Promise<Rollback> {
      const restoreChores = await snapshot<ChoreResponse[]>(queryClient, queryKeys.chores())
      const restoreMember = await snapshot<Member | null>(queryClient, memberKey)
      queryClient.setQueryData<ChoreResponse[]>(queryKeys.chores(), (old) =>
        old?.map((c) => (c.id === choreId ? markFreshlyCompleted(c) : c))
      )
      updateWallet((current) => current + points)
      return () => {
        restoreChores()
        restoreMember()
      }
    },

    choreCompletionRecorded({ chore, memberWalletBalance }: CompleteChoreResponse) {
      queryClient.setQueryData<ChoreResponse[]>(queryKeys.chores(), (old) =>
        old ? old.map((c) => (c.id === chore.id ? chore : c)) : [chore]
      )
      updateWallet(() => memberWalletBalance)
      invalidate(
        queryKeys.scoreboard(),
        queryKeys.choreReview(),
        queryKeys.choreActivity(),
        queryKeys.choreHistory(chore.id)
      )
    },

    choresChanged() {
      invalidate(queryKeys.chores(), queryKeys.choreReview())
    },

    async rewardPurchaseStarted(pointCost: number): Promise<Rollback> {
      const restoreMember = await snapshot<Member | null>(queryClient, memberKey)
      updateWallet((current) => Math.max(0, current - pointCost))
      return restoreMember
    },

    rewardPurchased({ memberWalletBalance }: BuyRewardResponse) {
      updateWallet(() => memberWalletBalance)
      invalidate(queryKeys.storeItems(), queryKeys.vouchers())
    },

    rewardItemsChanged() {
      invalidate(queryKeys.storeItems())
    },

    voucherRedeemed() {
      invalidate(queryKeys.vouchers())
    },
  }
}
