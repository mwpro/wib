import * as Dialog from '@radix-ui/react-dialog'
import { AlertTriangle } from 'lucide-react'
import type { ChoreResponse } from '../../types/chore'

interface DeleteChoreDialogProps {
  isOpen: boolean
  chore: ChoreResponse | null
  onClose: () => void
  onConfirm: () => Promise<void> | void
  isDeleting?: boolean
}

export function DeleteChoreDialog({
  isOpen,
  chore,
  onClose,
  onConfirm,
  isDeleting = false,
}: DeleteChoreDialogProps) {
  if (!isOpen || !chore) return null

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs animate-in fade-in" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-full max-w-sm bg-white dark:bg-[#14161d] rounded-2xl p-6 shadow-2xl border border-stone-200/90 dark:border-stone-800/90">
          <div className="flex flex-col items-center text-center gap-3">
            <div className="p-3 bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 rounded-xl">
              <AlertTriangle className="h-6 w-6" />
            </div>
            <Dialog.Title className="text-base font-black tracking-tight text-stone-950 dark:text-white">
              Usunąć zadanie?
            </Dialog.Title>
            <Dialog.Description className="text-sm text-stone-600 dark:text-stone-400 leading-relaxed">
              Czy na pewno chcesz usunąć zadanie{' '}
              <strong className="text-stone-950 dark:text-white font-semibold">
                {chore.title}
              </strong>
              ? Zostanie ono przeniesione do archiwum.
            </Dialog.Description>
          </div>

          <div className="flex items-center justify-end gap-2.5 mt-6">
            <button
              type="button"
              onClick={onClose}
              disabled={isDeleting}
              className="flex-1 py-2 px-4 text-xs font-semibold text-stone-700 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-xl transition cursor-pointer"
            >
              Anuluj
            </button>
            <button
              type="button"
              onClick={onConfirm}
              disabled={isDeleting}
              className="flex-1 py-2 px-4 bg-rose-600 hover:bg-rose-500 active:scale-95 text-white text-xs font-bold rounded-xl shadow-xs transition cursor-pointer disabled:opacity-50"
            >
              {isDeleting ? 'Usuwanie...' : 'Usuń'}
            </button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
