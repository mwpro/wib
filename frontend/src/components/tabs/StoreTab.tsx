import { useState } from 'react'
import {
  ShoppingBag,
  Ticket,
  History,
  Plus,
  ChevronDown,
  ChevronUp,
  RefreshCw,
  AlertCircle,
  Coins,
} from 'lucide-react'
import { useCurrentMember } from '../../hooks/useCurrentMember'
import {
  useStoreItems,
  usePurchaseReward,
  useCreateRewardItem,
  useUpdateRewardItem,
  useDeleteRewardItem,
} from '../../hooks/useStore'
import {
  useActiveVouchers,
  useRedeemedVouchers,
  useRedeemVoucher,
} from '../../hooks/useVouchers'
import { RewardCard } from '../store/RewardCard'
import { VoucherCard } from '../store/VoucherCard'
import { RedeemedVoucherCard } from '../store/RedeemedVoucherCard'
import { RewardFormModal } from '../store/RewardFormModal'
import { PurchaseConfirmDialog } from '../store/PurchaseConfirmDialog'
import { RedeemConfirmDialog } from '../store/RedeemConfirmDialog'
import { DeleteRewardDialog } from '../store/DeleteRewardDialog'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
} from '../../types/store'
import type { VoucherResponse } from '../../types/voucher'

export function StoreTab() {
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

  // Handlers for Reward management
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

  const handleConfirmDeleteReward = async () => {
    if (!rewardToDelete) return
    try {
      await deleteRewardMutation.mutateAsync(rewardToDelete.id)
      setRewardToDelete(null)
    } catch (err) {
      console.error('Failed to delete reward:', err)
    }
  }

  // Handlers for Purchasing
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
    } catch (err) {
      console.error('Failed to purchase reward:', err)
    }
  }

  // Handlers for Voucher Redemption
  const handleOpenRedeem = (voucher: VoucherResponse) => {
    setVoucherToRedeem(voucher)
  }

  const handleConfirmRedeem = async () => {
    if (!voucherToRedeem) return
    try {
      await redeemMutation.mutateAsync(voucherToRedeem.id)
      setVoucherToRedeem(null)
    } catch (err) {
      console.error('Failed to redeem voucher:', err)
    }
  }

  return (
    <div className="flex flex-col gap-5">
      {/* Top Header & Spendable Balance Banner */}
      <div className="flex items-center justify-between gap-3 flex-wrap">
        <div>
          <h2 className="text-xl font-black tracking-tight text-stone-900 dark:text-white flex items-center gap-2.5">
            <span>Sklep z nagrodami</span>
          </h2>
          <span className="text-xs text-stone-500 dark:text-stone-400">
            Wymieniaj punkty z zadań na nagrody i realizuj kupony
          </span>
        </div>

        <div className="flex items-center gap-2">
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-amber-100/90 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300/80 dark:border-amber-800/80 text-xs font-bold shadow-xs">
            <Coins className="h-3.5 w-3.5 text-amber-700 dark:text-amber-400" />
            <span>Twój portfel: {walletBalance} pkt</span>
          </span>

          <button
            type="button"
            onClick={handleOpenCreateReward}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 rounded-xl text-xs font-bold border border-amber-500/40 shadow-xs transition cursor-pointer"
          >
            <Plus className="h-4 w-4" />
            <span>Dodaj nagrodę</span>
          </button>
        </div>
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="p-12 flex flex-col items-center justify-center gap-3 text-stone-400">
          <RefreshCw className="h-6 w-6 animate-spin text-amber-500" />
          <span className="text-sm font-medium">Ładowanie nagród i portfela...</span>
        </div>
      )}

      {/* Error state */}
      {isError && (
        <div className="p-6 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-800 dark:text-rose-200 flex flex-col items-center gap-3 text-center">
          <AlertCircle className="h-8 w-8 text-rose-500" />
          <p className="text-sm font-medium">{errorMessage}</p>
          <button
            type="button"
            onClick={handleRefetchAll}
            className="px-4 py-1.5 bg-rose-100 dark:bg-rose-900/60 hover:bg-rose-200 dark:hover:bg-rose-900 rounded-lg text-xs font-semibold text-rose-900 dark:text-rose-100 transition cursor-pointer"
          >
            Spróbuj ponownie
          </button>
        </div>
      )}

      {/* Main Content when loaded without fatal error */}
      {!isLoading && !isError && (
        <>
          {/* SECTION 1: Mój portfel (Active Vouchers) */}
          <section className="flex flex-col gap-2.5">
            <button
              type="button"
              onClick={() => setIsWalletOpen(!isWalletOpen)}
              className="flex items-center justify-between p-1.5 -mx-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800/60 transition cursor-pointer text-left"
            >
              <div className="flex items-center gap-2">
                <Ticket className="h-4 w-4 text-amber-600 dark:text-amber-400" />
                <h3 className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
                  Mój portfel
                </h3>
                <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/60 dark:border-stone-700/60">
                  {activeVouchers.length}
                </span>
              </div>
              {isWalletOpen ? (
                <ChevronUp className="h-4 w-4 text-stone-400" />
              ) : (
                <ChevronDown className="h-4 w-4 text-stone-400" />
              )}
            </button>

            {isWalletOpen && (
              <div className="flex flex-col gap-2.5 animate-in fade-in duration-200">
                {activeVouchers.length > 0 ? (
                  activeVouchers.map((voucher) => (
                    <VoucherCard
                      key={voucher.id}
                      voucher={voucher}
                      onRedeem={handleOpenRedeem}
                      isRedeeming={
                        redeemMutation.isPending &&
                        voucherToRedeem?.id === voucher.id
                      }
                    />
                  ))
                ) : (
                  <div className="p-5 bg-white dark:bg-[#14161d] rounded-2xl border border-stone-200/80 dark:border-stone-800/80 text-center flex flex-col items-center gap-1.5 shadow-[0_1px_3px_rgba(0,0,0,0.02)]">
                    <div className="w-9 h-9 rounded-xl bg-stone-100 dark:bg-stone-800/80 text-stone-400 dark:text-stone-500 flex items-center justify-center">
                      <Ticket className="h-4.5 w-4.5" />
                    </div>
                    <p className="text-xs font-bold text-stone-800 dark:text-stone-200">
                      Twój portfel jest pusty
                    </p>
                    <p className="text-[11px] text-stone-500 dark:text-stone-400 max-w-xs">
                      Brak aktywnych kuponów. Wymień zdobyte punkty na nagrodę w sklepie poniżej!
                    </p>
                  </div>
                )}
              </div>
            )}
          </section>

          {/* SECTION 2: Sklep z nagrodami (Catalog) */}
          <section className="flex flex-col gap-2.5 pt-1">
            <button
              type="button"
              onClick={() => setIsStoreOpen(!isStoreOpen)}
              className="flex items-center justify-between p-1.5 -mx-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800/60 transition cursor-pointer text-left"
            >
              <div className="flex items-center gap-2">
                <ShoppingBag className="h-4 w-4 text-amber-600 dark:text-amber-400" />
                <h3 className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
                  Dostępne nagrody
                </h3>
                <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/60 dark:border-stone-700/60">
                  {rewards.length}
                </span>
              </div>
              {isStoreOpen ? (
                <ChevronUp className="h-4 w-4 text-stone-400" />
              ) : (
                <ChevronDown className="h-4 w-4 text-stone-400" />
              )}
            </button>

            {isStoreOpen && (
              <div className="animate-in fade-in duration-200">
                {rewards.length > 0 ? (
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    {rewards.map((reward) => (
                      <RewardCard
                        key={reward.id}
                        reward={reward}
                        walletBalance={walletBalance}
                        onPurchase={handleOpenPurchase}
                        onEdit={handleOpenEditReward}
                        onDelete={(r) => setRewardToDelete(r)}
                        isPurchasing={
                          purchaseMutation.isPending &&
                          rewardToPurchase?.id === reward.id
                        }
                      />
                    ))}
                  </div>
                ) : (
                  <div className="p-8 bg-white dark:bg-[#14161d] rounded-2xl border border-stone-200/90 dark:border-stone-800/80 text-center flex flex-col items-center gap-2.5 shadow-[0_2px_8px_rgba(0,0,0,0.02)]">
                    <div className="w-12 h-12 rounded-2xl bg-amber-100 dark:bg-amber-950/80 text-amber-800 dark:text-amber-400 flex items-center justify-center border border-amber-300 dark:border-amber-800">
                      <ShoppingBag className="h-6 w-6 text-amber-700 dark:text-amber-400" />
                    </div>
                    <h4 className="text-sm font-bold text-stone-900 dark:text-stone-100 tracking-tight">
                      Brak nagród w sklepie
                    </h4>
                    <p className="text-xs text-stone-500 dark:text-stone-400 max-w-sm leading-relaxed">
                      Dodaj pierwszą nagrodę lub przyjemność, na którą domownicy mogą wymieniać zdobyte punkty!
                    </p>
                    <button
                      type="button"
                      onClick={handleOpenCreateReward}
                      className="mt-1 inline-flex items-center gap-1.5 px-4 py-2 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 rounded-xl text-xs font-bold border border-amber-500/40 shadow-xs transition cursor-pointer"
                    >
                      <Plus className="h-4 w-4" />
                      <span>Dodaj pierwszą nagrodę</span>
                    </button>
                  </div>
                )}
              </div>
            )}
          </section>

          {/* SECTION 3: Historia zrealizowanych (Redeemed History) */}
          <section className="flex flex-col gap-2.5 pt-1">
            <button
              type="button"
              onClick={() => setIsHistoryOpen(!isHistoryOpen)}
              className="flex items-center justify-between p-1.5 -mx-1.5 rounded-xl hover:bg-stone-100 dark:hover:bg-stone-800/60 transition cursor-pointer text-left"
            >
              <div className="flex items-center gap-2">
                <History className="h-4 w-4 text-stone-400" />
                <h3 className="text-xs font-bold uppercase tracking-wider text-stone-500 dark:text-stone-400">
                  Historia zrealizowanych
                </h3>
                <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/60 dark:border-stone-700/60">
                  {redeemedVouchers.length}
                </span>
              </div>
              {isHistoryOpen ? (
                <ChevronUp className="h-4 w-4 text-stone-400" />
              ) : (
                <ChevronDown className="h-4 w-4 text-stone-400" />
              )}
            </button>

            {isHistoryOpen && (
              <div className="flex flex-col gap-2 animate-in fade-in duration-200">
                {redeemedVouchers.length > 0 ? (
                  redeemedVouchers.map((voucher) => (
                    <RedeemedVoucherCard key={voucher.id} voucher={voucher} />
                  ))
                ) : (
                  <div className="p-4 bg-white dark:bg-[#14161d] rounded-xl border border-stone-200/70 dark:border-stone-800/70 text-center text-xs text-stone-400 dark:text-stone-500">
                    Brak zrealizowanych kuponów w historii.
                  </div>
                )}
              </div>
            )}
          </section>
        </>
      )}

      {/* Modals & Dialogs */}
      <RewardFormModal
        isOpen={isRewardFormOpen}
        onClose={() => setIsRewardFormOpen(false)}
        onSubmit={handleSaveReward}
        rewardToEdit={rewardToEdit}
      />

      <PurchaseConfirmDialog
        isOpen={rewardToPurchase != null}
        reward={rewardToPurchase}
        onClose={() => setRewardToPurchase(null)}
        onConfirm={handleConfirmPurchase}
        isPurchasing={purchaseMutation.isPending}
      />

      <RedeemConfirmDialog
        isOpen={voucherToRedeem != null}
        voucher={voucherToRedeem}
        onClose={() => setVoucherToRedeem(null)}
        onConfirm={handleConfirmRedeem}
        isRedeeming={redeemMutation.isPending}
      />

      <DeleteRewardDialog
        isOpen={rewardToDelete != null}
        reward={rewardToDelete}
        onClose={() => setRewardToDelete(null)}
        onConfirm={handleConfirmDeleteReward}
        isDeleting={deleteRewardMutation.isPending}
      />
    </div>
  )
}
