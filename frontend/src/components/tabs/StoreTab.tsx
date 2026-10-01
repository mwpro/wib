import { RefreshCw, AlertCircle } from 'lucide-react'
import { useStoreTab } from '../../hooks/useStoreTab'
import { StoreHeader } from '../store/StoreHeader'
import { WalletSection } from '../store/WalletSection'
import { CatalogSection } from '../store/CatalogSection'
import { RedeemedHistorySection } from '../store/RedeemedHistorySection'
import { RewardFormModal } from '../store/RewardFormModal'
import { PurchaseConfirmDialog } from '../store/PurchaseConfirmDialog'
import { RedeemConfirmDialog } from '../store/RedeemConfirmDialog'
import { DeleteRewardDialog } from '../store/DeleteRewardDialog'

export function StoreTab() {
  const store = useStoreTab()

  return (
    <div className="flex flex-col gap-5">
      <StoreHeader
        walletBalance={store.walletBalance}
        onAddReward={store.handleOpenCreateReward}
      />

      {/* Loading state */}
      {store.isLoading && (
        <div className="p-12 flex flex-col items-center justify-center gap-3 text-stone-400">
          <RefreshCw className="h-6 w-6 animate-spin text-amber-500" />
          <span className="text-sm font-medium">Ładowanie nagród i portfela...</span>
        </div>
      )}

      {/* Query error state */}
      {store.isError && (
        <div className="p-6 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-800 dark:text-rose-200 flex flex-col items-center gap-3 text-center">
          <AlertCircle className="h-8 w-8 text-rose-500" />
          <p className="text-sm font-medium">{store.errorMessage}</p>
          <button
            type="button"
            onClick={store.handleRefetchAll}
            className="px-4 py-1.5 bg-rose-100 dark:bg-rose-900/60 hover:bg-rose-200 dark:hover:bg-rose-900 rounded-lg text-xs font-semibold text-rose-900 dark:text-rose-100 transition cursor-pointer"
          >
            Spróbuj ponownie
          </button>
        </div>
      )}

      {/* Main content */}
      {!store.isLoading && !store.isError && (
        <>
          <WalletSection
            activeVouchers={store.activeVouchers}
            isOpen={store.isWalletOpen}
            onToggle={() => store.setIsWalletOpen(!store.isWalletOpen)}
            onRedeem={store.handleOpenRedeem}
            redeemingVoucherId={
              store.isRedeeming ? (store.voucherToRedeem?.id ?? null) : null
            }
          />

          <CatalogSection
            rewards={store.rewards}
            isOpen={store.isStoreOpen}
            onToggle={() => store.setIsStoreOpen(!store.isStoreOpen)}
            walletBalance={store.walletBalance}
            onPurchase={store.handleOpenPurchase}
            onEdit={store.handleOpenEditReward}
            onDelete={store.handleOpenDeleteReward}
            onAddReward={store.handleOpenCreateReward}
            purchasingRewardId={
              store.isPurchasing ? (store.rewardToPurchase?.id ?? null) : null
            }
          />

          <RedeemedHistorySection
            redeemedVouchers={store.redeemedVouchers}
            isOpen={store.isHistoryOpen}
            onToggle={() => store.setIsHistoryOpen(!store.isHistoryOpen)}
          />
        </>
      )}

      {/* Modals & Dialogs */}
      <RewardFormModal
        isOpen={store.isRewardFormOpen}
        onClose={store.closeRewardForm}
        onSubmit={store.handleSaveReward}
        rewardToEdit={store.rewardToEdit}
      />

      <PurchaseConfirmDialog
        isOpen={store.rewardToPurchase != null}
        reward={store.rewardToPurchase}
        onClose={store.closePurchaseDialog}
        onConfirm={store.handleConfirmPurchase}
        isPurchasing={store.isPurchasing}
        error={store.purchaseError?.message ?? null}
      />

      <RedeemConfirmDialog
        isOpen={store.voucherToRedeem != null}
        voucher={store.voucherToRedeem}
        onClose={store.closeRedeemDialog}
        onConfirm={store.handleConfirmRedeem}
        isRedeeming={store.isRedeeming}
        error={store.redeemError?.message ?? null}
      />

      <DeleteRewardDialog
        isOpen={store.rewardToDelete != null}
        reward={store.rewardToDelete}
        onClose={store.closeDeleteDialog}
        onConfirm={store.handleConfirmDeleteReward}
        isDeleting={store.isDeleting}
        error={store.deleteError?.message ?? null}
      />
    </div>
  )
}
