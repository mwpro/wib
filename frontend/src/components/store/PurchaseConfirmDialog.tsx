import * as Dialog from '@radix-ui/react-dialog'
import { Coins, AlertCircle } from 'lucide-react'
import type { RewardItemResponse } from '../../types/store'

interface PurchaseConfirmDialogProps {
  isOpen: boolean
  reward: RewardItemResponse | null
  onClose: () => void
  onConfirm: () => Promise<void> | void
  isPurchasing?: boolean
  error?: string | null
}

export function PurchaseConfirmDialog({
  isOpen,
  reward,
  onClose,
  onConfirm,
  isPurchasing = false,
  error = null,
}: PurchaseConfirmDialogProps) {
  if (!isOpen || !reward) return null

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs animate-in fade-in" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-full max-w-sm bg-white dark:bg-[#14161d] rounded-2xl p-6 shadow-2xl border border-stone-200/90 dark:border-stone-800/90">
          <div className="flex flex-col items-center text-center gap-3">
            <div className="p-3 bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/20 rounded-xl">
              <Coins className="h-6 w-6" />
            </div>
            <Dialog.Title className="text-base font-black tracking-tight text-stone-950 dark:text-white">
              Potwierdź zakup nagrody
            </Dialog.Title>
            <Dialog.Description className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed">
              Czy na pewno chcesz wymienić{' '}
              <strong className="text-amber-700 dark:text-amber-400 font-bold">
                {reward.pointCost} pkt
              </strong>{' '}
              na nagrodę{' '}
              <strong className="text-stone-950 dark:text-white font-semibold">
                {reward.title}
              </strong>
              ? Punkty zostaną pobrane z Twojego portfela.
            </Dialog.Description>
          </div>

          {error && (
            <div className="mt-4 p-3 text-xs rounded-xl bg-rose-50 dark:bg-rose-950/40 text-rose-800 dark:text-rose-300 border border-rose-200 dark:border-rose-900 flex items-start gap-2">
              <AlertCircle className="h-4 w-4 text-rose-500 shrink-0 mt-px" />
              <span>{error}</span>
            </div>
          )}

          <div className="flex items-center justify-end gap-2.5 mt-6">
            <button
              type="button"
              onClick={onClose}
              disabled={isPurchasing}
              className="flex-1 py-2 px-4 text-xs font-semibold text-stone-700 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-xl transition cursor-pointer"
            >
              Anuluj
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={isPurchasing}
              className="flex-1 py-2 px-4 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 text-xs font-bold rounded-xl border border-amber-500/40 shadow-xs transition cursor-pointer disabled:opacity-50"
            >
              {isPurchasing ? 'Kupowanie...' : 'Kupuję nagrodę'}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
