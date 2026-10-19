import * as Dialog from '@radix-ui/react-dialog'
import { Check, AlertCircle } from 'lucide-react'
import type { ConfirmedActionDialogProps } from '../../hooks/useConfirmedAction'
import type { ChoreResponse } from '../../types/chore'

interface RepeatCompletionDialogProps extends ConfirmedActionDialogProps {
  chore: ChoreResponse | null
}

export function RepeatCompletionDialog({
  isOpen,
  chore,
  onClose,
  onConfirm,
  isPending,
  error,
}: RepeatCompletionDialogProps) {
  if (!isOpen || !chore) return null

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs animate-in fade-in" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-[calc(100%-2rem)] max-w-sm bg-white dark:bg-[#14161d] rounded-2xl p-6 shadow-2xl border border-stone-200/90 dark:border-stone-800/90">
          <div className="flex flex-col items-center text-center gap-3">
            <div className="p-3 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 rounded-xl">
              <Check className="h-6 w-6 stroke-[3]" />
            </div>
            <Dialog.Title className="text-base font-black tracking-tight text-stone-950 dark:text-white">
              Zrobione ponownie?
            </Dialog.Title>
            <Dialog.Description className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed">
              <strong className="text-stone-950 dark:text-white font-semibold">{chore.title}</strong>{' '}
              jest już dziś odhaczone. Zapisać kolejne wykonanie? Otrzymasz{' '}
              <strong className="text-emerald-700 dark:text-emerald-400 font-bold">
                +{chore.points} pkt
              </strong>
              .
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
              disabled={isPending}
              className="flex-1 py-2 px-4 text-xs font-semibold text-stone-700 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-xl transition cursor-pointer"
            >
              Anuluj
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={isPending}
              className="flex-1 py-2 px-4 bg-emerald-600 hover:bg-emerald-500 active:scale-95 text-white text-xs font-bold rounded-xl shadow-xs transition cursor-pointer disabled:opacity-50"
            >
              {isPending ? 'Zapisywanie...' : 'Zapisz ponownie'}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
