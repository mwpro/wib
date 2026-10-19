import { useState } from 'react'
import { useCurrentMember } from './useCurrentMember'
import { useConfirmedAction } from './useConfirmedAction'
import {
  useStoreItems,
  usePurchaseReward,
  useCreateRewardItem,
  useUpdateRewardItem,
  useDeleteRewardItem,
} from './useStore'
import {
  useActiveVouchers,
  useRedeemedVouchers,
  useRedeemVoucher,
} from './useVouchers'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
} from '../types/store'
import type { VoucherResponse } from '../types/voucher'

export function useStoreTab() {
  const { currentMember } = useCurrentMember()
  const walletBalance = currentMember?.walletBalance ?? 0

  // Store queries and mutations
  const {
    data: rewards = [],
    isLoading: isLoadingRewards,
    isError: isErrorRewards,
    error: errorRewards,
    refetch: refetchRewards,
  } = useStoreItems()
  const purchaseMutation = usePurchaseReward()
  const createRewardMutation = useCreateRewardItem()
  const updateRewardMutation = useUpdateRewardItem()
  const deleteRewardMutation = useDeleteRewardItem()

  // Voucher queries and mutations
  const {
    data: activeVouchers = [],
    isLoading: isLoadingActive,
    isError: isErrorActive,
    error: errorActive,
    refetch: refetchActive,
  } = useActiveVouchers()
  const {
    data: redeemedVouchers = [],
    isLoading: isLoadingRedeemed,
    isError: isErrorRedeemed,
    refetch: refetchRedeemed,
  } = useRedeemedVouchers()
  const redeemMutation = useRedeemVoucher()

  // Section collapse states
  const [isWalletOpen, setIsWalletOpen] = useState(true)
  const [isStoreOpen, setIsStoreOpen] = useState(true)
  const [isHistoryOpen, setIsHistoryOpen] = useState(false)

  // Reward form state
  const [isRewardFormOpen, setIsRewardFormOpen] = useState(false)
  const [rewardToEdit, setRewardToEdit] = useState<RewardItemResponse | null>(null)

  const purchase = useConfirmedAction((reward: RewardItemResponse) =>
    purchaseMutation.mutateAsync({ rewardItemId: reward.id, pointCost: reward.pointCost })
  )
  const redeem = useConfirmedAction((voucher: VoucherResponse) =>
    redeemMutation.mutateAsync(voucher.id)
  )
  const deleteReward = useConfirmedAction((reward: RewardItemResponse) =>
    deleteRewardMutation.mutateAsync(reward.id)
  )

  const isLoading = isLoadingRewards || isLoadingActive || isLoadingRedeemed
  const isError = isErrorRewards || isErrorActive || isErrorRedeemed
  const errorMessage =
    (errorRewards instanceof Error && errorRewards.message) ||
    (errorActive instanceof Error && errorActive.message) ||
    'Wystąpił błąd podczas ładowania sklepu.'

  const handleRefetchAll = () => {
    refetchRewards()
    refetchActive()
    refetchRedeemed()
  }

  const handleOpenCreateReward = () => {
    setRewardToEdit(null)
    setIsRewardFormOpen(true)
  }

  const handleOpenEditReward = (reward: RewardItemResponse) => {
    setRewardToEdit(reward)
    setIsRewardFormOpen(true)
  }

  const handleSaveReward = async (data: CreateRewardItemRequest | UpdateRewardItemRequest) => {
    if (rewardToEdit) {
      await updateRewardMutation.mutateAsync({ id: rewardToEdit.id, req: data })
    } else {
      await createRewardMutation.mutateAsync(data)
    }
  }

  const closeRewardForm = () => setIsRewardFormOpen(false)

  return {
    // Data
    walletBalance,
    rewards,
    activeVouchers,
    redeemedVouchers,

    // Loading / query error
    isLoading,
    isError,
    errorMessage,
    handleRefetchAll,

    // Section collapse
    isWalletOpen,
    setIsWalletOpen,
    isStoreOpen,
    setIsStoreOpen,
    isHistoryOpen,
    setIsHistoryOpen,

    // Reward form
    isRewardFormOpen,
    rewardToEdit,
    handleOpenCreateReward,
    handleOpenEditReward,
    handleSaveReward,
    closeRewardForm,

    // Confirmed actions
    purchase,
    redeem,
    deleteReward,
  }
}
