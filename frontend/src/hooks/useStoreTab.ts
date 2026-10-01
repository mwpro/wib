import { useState } from 'react'
import { useCurrentMember } from './useCurrentMember'
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

  // Dialog & Modal states
  const [isRewardFormOpen, setIsRewardFormOpen] = useState(false)
  const [rewardToEdit, setRewardToEdit] = useState<RewardItemResponse | null>(null)
  const [rewardToPurchase, setRewardToPurchase] = useState<RewardItemResponse | null>(null)
  const [rewardToDelete, setRewardToDelete] = useState<RewardItemResponse | null>(null)
  const [voucherToRedeem, setVoucherToRedeem] = useState<VoucherResponse | null>(null)

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

  // --- Reward form handlers ---

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

  // --- Purchase handlers ---

  const handleOpenPurchase = (reward: RewardItemResponse) => {
    setRewardToPurchase(reward)
  }

  const handleConfirmPurchase = async () => {
    if (!rewardToPurchase) return
    try {
      await purchaseMutation.mutateAsync({
        rewardItemId: rewardToPurchase.id,
        pointCost: rewardToPurchase.pointCost,
      })
      setRewardToPurchase(null)
    } catch {
      // Error is captured in purchaseMutation.error and shown in the dialog
    }
  }

  const closePurchaseDialog = () => {
    setRewardToPurchase(null)
    purchaseMutation.reset()
  }

  // --- Redeem handlers ---

  const handleOpenRedeem = (voucher: VoucherResponse) => {
    setVoucherToRedeem(voucher)
  }

  const handleConfirmRedeem = async () => {
    if (!voucherToRedeem) return
    try {
      await redeemMutation.mutateAsync(voucherToRedeem.id)
      setVoucherToRedeem(null)
    } catch {
      // Error is captured in redeemMutation.error and shown in the dialog
    }
  }

  const closeRedeemDialog = () => {
    setVoucherToRedeem(null)
    redeemMutation.reset()
  }

  // --- Delete handlers ---

  const handleOpenDeleteReward = (reward: RewardItemResponse) => {
    setRewardToDelete(reward)
  }

  const handleConfirmDeleteReward = async () => {
    if (!rewardToDelete) return
    try {
      await deleteRewardMutation.mutateAsync(rewardToDelete.id)
      setRewardToDelete(null)
    } catch {
      // Error is captured in deleteRewardMutation.error and shown in the dialog
    }
  }

  const closeDeleteDialog = () => {
    setRewardToDelete(null)
    deleteRewardMutation.reset()
  }

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

    // Purchase dialog
    rewardToPurchase,
    handleOpenPurchase,
    handleConfirmPurchase,
    closePurchaseDialog,
    purchaseError: purchaseMutation.error,
    isPurchasing: purchaseMutation.isPending,

    // Redeem dialog
    voucherToRedeem,
    handleOpenRedeem,
    handleConfirmRedeem,
    closeRedeemDialog,
    redeemError: redeemMutation.error,
    isRedeeming: redeemMutation.isPending,

    // Delete dialog
    rewardToDelete,
    handleOpenDeleteReward,
    handleConfirmDeleteReward,
    closeDeleteDialog,
    deleteError: deleteRewardMutation.error,
    isDeleting: deleteRewardMutation.isPending,
  }
}
