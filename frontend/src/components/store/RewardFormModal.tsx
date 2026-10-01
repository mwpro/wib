import { useState } from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Gift, Coins } from 'lucide-react'
import type {
  RewardItemResponse,
  CreateRewardItemRequest,
  UpdateRewardItemRequest,
} from '../../types/store'

interface RewardFormModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: CreateRewardItemRequest | UpdateRewardItemRequest) => Promise<void>
  rewardToEdit?: RewardItemResponse | null
}

const POINT_PRESETS = [10, 25, 50, 100, 200]

export function RewardFormModal({
  isOpen,
  onClose,
  onSubmit,
  rewardToEdit,
}: RewardFormModalProps) {
  if (!isOpen) return null

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs animate-in fade-in" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-full max-w-lg max-h-[90vh] overflow-y-auto bg-white dark:bg-[#14161d] rounded-2xl p-6 shadow-2xl border border-stone-200/90 dark:border-stone-800/90 flex flex-col gap-5">
          <InnerRewardForm
            key={rewardToEdit?.id ?? 'new'}
            rewardToEdit={rewardToEdit}
            onClose={onClose}
            onSubmit={onSubmit}
          />
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}

interface InnerFormProps {
  rewardToEdit?: RewardItemResponse | null
  onClose: () => void
  onSubmit: (data: CreateRewardItemRequest | UpdateRewardItemRequest) => Promise<void>
}

function InnerRewardForm({ rewardToEdit, onClose, onSubmit }: InnerFormProps) {
  const [title, setTitle] = useState(rewardToEdit?.title ?? '')
  const [pointCost, setPointCost] = useState<number | ''>(rewardToEdit?.pointCost ?? 50)
  const [description, setDescription] = useState(rewardToEdit?.description ?? '')
  const [quantity, setQuantity] = useState<number | ''>(rewardToEdit?.quantity ?? '')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const trimmedTitle = title.trim()

    if (!trimmedTitle) {
      setError('Wpisz nazwę nagrody.')
      return
    }

    if (trimmedTitle.length > 255) {
      setError('Nazwa nagrody może mieć maksymalnie 255 znaków.')
      return
    }

    const costNum = Number(pointCost)
    if (!pointCost || isNaN(costNum) || costNum < 1) {
      setError('Koszt nagrody musi wynosić co najmniej 1 punkt.')
      return
    }

    let parsedQuantity: number | null = null
    if (quantity !== '' && quantity !== null) {
      const qNum = Number(quantity)
      if (isNaN(qNum) || qNum < 1) {
        setError('Liczba sztuk musi wynosić co najmniej 1.')
        return
      }
      parsedQuantity = qNum
    }

    if (description && description.trim().length > 2000) {
      setError('Opis może mieć maksymalnie 2000 znaków.')
      return
    }

    setError(null)
    setIsSubmitting(true)

    try {
      await onSubmit({
        title: trimmedTitle,
        pointCost: costNum,
        description: description.trim() || null,
        quantity: parsedQuantity,
      })
      onClose()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Wystąpił błąd podczas zapisywania nagrody.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="flex items-center justify-between pb-3 border-b border-stone-100 dark:border-stone-800">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-xl bg-amber-100 dark:bg-amber-950/80 text-amber-900 dark:text-amber-300 border border-amber-300 dark:border-amber-800 flex items-center justify-center">
            <Gift className="w-4 h-4" />
          </div>
          <div>
            <Dialog.Title className="text-base font-bold text-stone-900 dark:text-stone-100 tracking-tight">
              {rewardToEdit ? 'Edytuj nagrodę' : 'Dodaj nową nagrodę'}
            </Dialog.Title>
            <Dialog.Description className="text-xs text-stone-500 dark:text-stone-400">
              {rewardToEdit
                ? 'Zmień parametry nagrody w sklepie domowym'
                : 'Stwórz nagrodę, na którą domownicy mogą wymieniać punkty'}
            </Dialog.Description>
          </div>
        </div>
        <button
          type="button"
          onClick={onClose}
          className="p-1 rounded-lg text-stone-400 hover:text-stone-600 dark:hover:text-stone-200 hover:bg-stone-100 dark:hover:bg-stone-800 transition cursor-pointer"
        >
          <X className="h-5 w-5" />
        </button>
      </div>

      {error && (
        <div className="p-3 text-xs rounded-xl bg-rose-50 dark:bg-rose-950/40 text-rose-800 dark:text-rose-300 border border-rose-200 dark:border-rose-900">
          {error}
        </div>
      )}

      {/* Field 1: Title */}
      <div className="flex flex-col gap-1.5">
        <label
          htmlFor="reward-title"
          className="text-xs font-bold text-stone-700 dark:text-stone-300 uppercase tracking-wider"
        >
          Nazwa nagrody *
        </label>
        <input
          id="reward-title"
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="np. Masaż pleców, Wyjście solo, Kupno książki"
          maxLength={255}
          className="w-full px-3.5 py-2 text-sm rounded-xl border border-stone-200 dark:border-stone-700 bg-stone-50 dark:bg-stone-900 text-stone-900 dark:text-stone-100 placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 transition"
        />
      </div>

      {/* Field 2: Point Cost */}
      <div className="flex flex-col gap-2">
        <label
          htmlFor="reward-cost"
          className="text-xs font-bold text-stone-700 dark:text-stone-300 uppercase tracking-wider flex items-center justify-between"
        >
          <span>Koszt w punktach *</span>
          <span className="text-[11px] font-normal text-stone-500">min. 1 pkt</span>
        </label>

        <div className="flex items-center gap-2">
          <div className="relative flex-1">
            <Coins className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-amber-500" />
            <input
              id="reward-cost"
              type="number"
              min={1}
              value={pointCost}
              onChange={(e) => setPointCost(e.target.value === '' ? '' : Math.max(1, parseInt(e.target.value, 10)))}
              className="w-full pl-9 pr-3.5 py-2 text-sm font-semibold rounded-xl border border-stone-200 dark:border-stone-700 bg-stone-50 dark:bg-stone-900 text-stone-900 dark:text-stone-100 focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 transition"
            />
          </div>
          <div className="flex items-center gap-1">
            {POINT_PRESETS.map((pts) => (
              <button
                key={pts}
                type="button"
                onClick={() => setPointCost(pts)}
                className={`px-2.5 py-2 text-xs font-semibold rounded-xl border transition cursor-pointer select-none ${
                  pointCost === pts
                    ? 'bg-amber-400 text-amber-950 border-amber-500/40 shadow-xs'
                    : 'bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-300 border-stone-200 dark:border-stone-700 hover:border-amber-400'
                }`}
              >
                {pts}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Field 3: Quantity */}
      <div className="flex flex-col gap-1.5">
        <label
          htmlFor="reward-quantity"
          className="text-xs font-bold text-stone-700 dark:text-stone-300 uppercase tracking-wider flex items-center justify-between"
        >
          <span>Liczba sztuk (limit dostępności)</span>
          <span className="text-[11px] font-normal text-stone-500">Puste = bez limitu</span>
        </label>
        <input
          id="reward-quantity"
          type="number"
          min={1}
          value={quantity}
          onChange={(e) =>
            setQuantity(e.target.value === '' ? '' : Math.max(1, parseInt(e.target.value, 10)))
          }
          placeholder="np. 1 lub 3 (pozostaw puste, jeśli nielimitowane)"
          className="w-full px-3.5 py-2 text-sm rounded-xl border border-stone-200 dark:border-stone-700 bg-stone-50 dark:bg-stone-900 text-stone-900 dark:text-stone-100 placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 transition"
        />
      </div>

      {/* Field 4: Description */}
      <div className="flex flex-col gap-1.5">
        <label
          htmlFor="reward-description"
          className="text-xs font-bold text-stone-700 dark:text-stone-300 uppercase tracking-wider"
        >
          Opis nagrody (opcjonalnie)
        </label>
        <textarea
          id="reward-description"
          rows={3}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Szczegóły nagrody, warunki realizacji..."
          maxLength={2000}
          className="w-full px-3.5 py-2 text-sm rounded-xl border border-stone-200 dark:border-stone-700 bg-stone-50 dark:bg-stone-900 text-stone-900 dark:text-stone-100 placeholder:text-stone-400 focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 transition resize-none"
        />
      </div>

      <div className="flex items-center justify-end gap-2.5 pt-3 border-t border-stone-100 dark:border-stone-800 mt-2">
        <button
          type="button"
          onClick={onClose}
          disabled={isSubmitting}
          className="px-4 py-2 text-xs font-semibold text-stone-600 dark:text-stone-400 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-xl transition cursor-pointer"
        >
          Anuluj
        </button>
        <button
          type="submit"
          disabled={isSubmitting}
          className="px-5 py-2 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 text-xs font-bold rounded-xl border border-amber-500/40 shadow-xs transition cursor-pointer disabled:opacity-50"
        >
          {isSubmitting
            ? 'Zapisywanie...'
            : rewardToEdit
            ? 'Zapisz zmiany'
            : 'Dodaj nagrodę'}
        </button>
      </div>
    </form>
  )
}
