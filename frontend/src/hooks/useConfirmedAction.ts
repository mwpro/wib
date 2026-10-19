import { useState } from 'react'

export function useConfirmedAction<T>(perform: (target: T) => Promise<unknown>) {
  const [target, setTarget] = useState<T | null>(null)
  const [isPending, setIsPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const open = (next: T) => {
    setError(null)
    setTarget(next)
  }

  const close = () => {
    if (isPending) return
    setTarget(null)
    setError(null)
  }

  const confirm = async () => {
    if (target == null) return
    setError(null)
    setIsPending(true)
    try {
      await perform(target)
      setTarget(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Wystąpił nieoczekiwany błąd.')
    } finally {
      setIsPending(false)
    }
  }

  return {
    target,
    pendingTarget: isPending ? target : null,
    open,
    dialog: {
      isOpen: target != null,
      onClose: close,
      onConfirm: confirm,
      isPending,
      error,
    },
  }
}

export interface ConfirmedActionDialogProps {
  isOpen: boolean
  onClose: () => void
  onConfirm: () => Promise<void> | void
  isPending: boolean
  error: string | null
}
