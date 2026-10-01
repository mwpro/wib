import * as Dialog from '@radix-ui/react-dialog'
import { Sparkles } from 'lucide-react'
import type { VoucherResponse } from '../../types/voucher'

interface RedeemConfirmDialogProps {
  isOpen: boolean
  voucher: VoucherResponse | null
  onClose: () => void
  onConfirm: () => Promise<void> | void
  isRedeeming?: boolean
}

export function RedeemConfirmDialog({
  isOpen,
  voucher,
  onClose,
  onConfirm,
  isRedeeming = false,
}: RedeemConfirmDialogProps) {
  if (!isOpen || !voucher) return null

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs animate-in fade-in" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-full max-w-sm bg-white dark:bg-[#14161d] rounded-2xl p-6 shadow-2xl border border-stone-200/90 dark:border-stone-800/90">
          <div className="flex flex-col items-center text-center gap-3">
            <div className="p-3 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 rounded-xl">
              <Sparkles className="h-6 w-6" />
            </div>
            <Dialog.Title className="text-base font-black tracking-tight text-stone-950 dark:text-white">
              Realizacja kuponu
            </Dialog.Title>
            <Dialog.Description className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed">
              Czy na pewno chcesz oznaczyć kupon{' '}
              <strong className="text-stone-950 dark:text-white font-semibold">
                {voucher.titleSnapshot}
              </strong>{' '}
              jako zrealizowany? Zostanie on przeniesiony do historii.
            </Dialog.Description>
          </div>

          <div className="flex items-center justify-end gap-2.5 mt-6">
            <button
              type="button"
              onClick={onClose}
              disabled={isRedeeming}
              className="flex-1 py-2 px-4 text-xs font-semibold text-stone-700 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-xl transition cursor-pointer"
            >
              Anuluj
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={isRedeeming}
              className="flex-1 py-2 px-4 bg-emerald-600 hover:bg-emerald-500 active:scale-95 text-white text-xs font-bold rounded-xl shadow-xs transition cursor-pointer disabled:opacity-50"
            >
              {isRedeeming ? 'Realizowanie...' : 'Zrealizuj kupon'}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
