import { useState } from 'react'
import * as Dialog from '@radix-ui/react-dialog'
import { X, Plus, Calendar, CheckSquare } from 'lucide-react'
import type { ChoreResponse, CreateChoreRequest, UpdateChoreRequest } from '../../types/chore'

export interface InitialChoreValues {
  title?: string
  cadenceDays?: number | null
  tags?: string[]
}

interface ChoreFormModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: CreateChoreRequest | UpdateChoreRequest) => Promise<void>
  choreToEdit?: ChoreResponse | null
  existingTags: string[]
  initialValues?: InitialChoreValues
}

const CADENCE_PRESETS = [
  { label: 'Codziennie', days: 1 },
  { label: 'Co 3 dni', days: 3 },
  { label: 'Co tydzień', days: 7 },
  { label: 'Co 2 tyg.', days: 14 },
  { label: 'Co miesiąc', days: 30 },
  { label: 'Co 2 mies.', days: 60 },
  { label: 'Co pół roku', days: 180 },
  { label: 'Co rok', days: 365 },
]

interface InnerFormProps {
  choreToEdit?: ChoreResponse | null
  existingTags: string[]
  initialValues?: InitialChoreValues
  onClose: () => void
  onSubmit: (data: CreateChoreRequest | UpdateChoreRequest) => Promise<void>
}

function InnerChoreForm({
  choreToEdit,
  existingTags,
  initialValues,
  onClose,
  onSubmit,
}: InnerFormProps) {
  const [title, setTitle] = useState(
    choreToEdit?.title ?? initialValues?.title ?? ''
  )
  const [description, setDescription] = useState(choreToEdit?.description ?? '')
  const [isScheduled, setIsScheduled] = useState(() => {
    if (choreToEdit) return choreToEdit.cadenceDays != null
    if (initialValues && initialValues.cadenceDays !== undefined) {
      return initialValues.cadenceDays != null
    }
    return true
  })
  const [cadenceDays, setCadenceDays] = useState<number | ''>(() => {
    if (choreToEdit) return choreToEdit.cadenceDays ?? 7
    if (initialValues && initialValues.cadenceDays !== undefined) {
      return initialValues.cadenceDays ?? 7
    }
    return 7
  })
  const [selectedTags, setSelectedTags] = useState<string[]>(() => {
    if (choreToEdit?.tags) return [...choreToEdit.tags]
    if (initialValues?.tags) return [...initialValues.tags]
    return []
  })
  const [newTagInput, setNewTagInput] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleToggleTag = (tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    )
  }

  const handleAddNewTag = () => {
    const trimmed = newTagInput.trim().toLowerCase().replace(/^#/, '')
    if (!trimmed) return
    if (!selectedTags.includes(trimmed)) {
      setSelectedTags((prev) => [...prev, trimmed])
    }
    setNewTagInput('')
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!title.trim()) {
      setError('Tytuł zadania nie może być pusty.')
    }

    if (isScheduled && (!cadenceDays || Number(cadenceDays) < 1)) {
      setError('Częstotliwość musi wynosić co najmniej 1 dzień.')
      return
    }

    try {
      setIsSubmitting(true)
      setError(null)

      const payload: CreateChoreRequest = {
        title: title.trim(),
        description: description.trim() ? description.trim() : null,
        points: choreToEdit ? choreToEdit.points : 1,
        cadenceDays: isScheduled ? Number(cadenceDays) : null,
        tags: selectedTags,
      }

      await onSubmit(payload)
      onClose()
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Wystąpił błąd podczas zapisywania.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="mt-4 flex flex-col gap-5">
      {error && (
        <div className="p-3 rounded-xl bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 text-sm border border-rose-200 dark:border-rose-900">
          {error}
        </div>
      )}

      {/* Title */}
      <div className="flex flex-col gap-1.5">
        <label htmlFor="chore-title" className="text-xs font-semibold text-stone-700 dark:text-stone-300 uppercase tracking-wider">
          Nazwa zadania *
        </label>
        <input
          id="chore-title"
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="np. Opróżnić zmywarkę"
          maxLength={255}
          required
          className="w-full px-3.5 py-2.5 bg-stone-50 dark:bg-stone-800/60 border border-stone-200 dark:border-stone-700 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-400 dark:focus:border-amber-500 transition"
        />
      </div>

      {/* Description */}
      <div className="flex flex-col gap-1.5">
        <label htmlFor="chore-desc" className="text-xs font-semibold text-stone-700 dark:text-stone-300 uppercase tracking-wider">
          Opis (opcjonalnie)
        </label>
        <textarea
          id="chore-desc"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Wskazówki lub dodatkowe informacje..."
          rows={2}
          maxLength={2000}
          className="w-full px-3.5 py-2.5 bg-stone-50 dark:bg-stone-800/60 border border-stone-200 dark:border-stone-700 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-400 dark:focus:border-amber-500 transition resize-none"
        />
      </div>

      {/* Cadence Type Toggle */}
      <div className="flex flex-col gap-2">
        <span className="text-xs font-semibold text-stone-700 dark:text-stone-300 uppercase tracking-wider">
          Częstotliwość wykonania
        </span>
        <div className="grid grid-cols-2 gap-2 p-1 bg-stone-100 dark:bg-stone-800 rounded-xl text-xs font-semibold">
          <button
            type="button"
            onClick={() => setIsScheduled(true)}
            className={`py-2 px-3 rounded-lg flex items-center justify-center gap-1.5 transition cursor-pointer ${
              isScheduled
                ? 'bg-white dark:bg-stone-900 text-stone-900 dark:text-stone-100 shadow-xs'
                : 'text-stone-500 hover:text-stone-900 dark:hover:text-stone-100'
            }`}
          >
            <Calendar className="h-3.5 w-3.5 text-amber-600 dark:text-amber-400" />
            <span>Cykliczne</span>
          </button>
          <button
            type="button"
            onClick={() => setIsScheduled(false)}
            className={`py-2 px-3 rounded-lg flex items-center justify-center gap-1.5 transition cursor-pointer ${
              !isScheduled
                ? 'bg-white dark:bg-stone-900 text-stone-900 dark:text-stone-100 shadow-xs'
                : 'text-stone-500 hover:text-stone-900 dark:hover:text-stone-100'
            }`}
          >
            <CheckSquare className="h-3.5 w-3.5 text-stone-400" />
            <span>Bez terminu</span>
          </button>
        </div>

        {/* Cadence Presets & Input */}
        {isScheduled && (
          <div className="flex flex-col gap-2 mt-1">
            <div className="flex items-center gap-1.5 flex-wrap">
              {CADENCE_PRESETS.map((preset) => (
                <button
                  key={preset.days}
                  type="button"
                  onClick={() => setCadenceDays(preset.days)}
                  className={`px-2.5 py-1.5 rounded-lg text-xs transition cursor-pointer ${
                    cadenceDays === preset.days
                      ? 'bg-amber-500 text-stone-950 font-bold shadow-xs'
                      : 'bg-stone-100 dark:bg-stone-800 text-stone-700 dark:text-stone-300 hover:bg-stone-200 dark:hover:bg-stone-700 font-medium'
                  }`}
                >
                  {preset.label}
                </button>
              ))}
            </div>

            <div className="flex items-center gap-2 mt-1">
              <span className="text-xs text-stone-500 dark:text-stone-400 font-medium">Co ile dni:</span>
              <input
                type="number"
                min={1}
                max={365}
                value={cadenceDays}
                onChange={(e) =>
                  setCadenceDays(e.target.value === '' ? '' : Math.max(1, parseInt(e.target.value) || 1))
                }
                className="w-20 px-3 py-1.5 bg-stone-50 dark:bg-stone-900/60 border border-stone-200 dark:border-stone-700 rounded-lg text-sm text-center font-bold focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-400"
              />
              <span className="text-xs text-stone-500 dark:text-stone-400 font-medium">dni</span>
            </div>
          </div>
        )}
      </div>

      {/* Tags */}
      <div className="flex flex-col gap-2">
        <span className="text-[11px] font-bold uppercase tracking-wider text-stone-400 dark:text-stone-500">
          Kategorie (tagi)
        </span>

        {/* Existing tags to toggle */}
        {existingTags.length > 0 && (
          <div className="flex items-center gap-1.5 flex-wrap">
            {existingTags.map((tag) => {
              const isSelected = selectedTags.includes(tag)
              return (
                <button
                  key={tag}
                  type="button"
                  onClick={() => handleToggleTag(tag)}
                  className={`px-2.5 py-1 rounded-lg text-xs transition cursor-pointer ${
                    isSelected
                      ? 'bg-amber-100 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300 font-bold shadow-xs'
                      : 'bg-stone-100 dark:bg-stone-800 text-stone-700 dark:text-stone-300 hover:bg-stone-200 dark:hover:bg-stone-700 font-medium'
                  }`}
                >
                  #{tag}
                </button>
              )
            })}
          </div>
        )}

        {/* Add custom tag */}
        <div className="flex items-center gap-2 mt-1">
          <input
            type="text"
            value={newTagInput}
            onChange={(e) => setNewTagInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                e.preventDefault()
                handleAddNewTag()
              }
            }}
            placeholder="Nowy tag (np. kuchnia)"
            maxLength={50}
            className="flex-1 px-3 py-1.5 bg-stone-50 dark:bg-stone-900/60 border border-stone-200 dark:border-stone-700 rounded-lg text-xs focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-400"
          />
          <button
            type="button"
            onClick={handleAddNewTag}
            className="px-3 py-1.5 bg-stone-200 dark:bg-stone-800 text-stone-800 dark:text-stone-200 hover:bg-stone-300 dark:hover:bg-stone-700 rounded-lg text-xs font-bold flex items-center gap-1 transition cursor-pointer"
          >
            <Plus className="h-3 w-3" />
            <span>Dodaj</span>
          </button>
        </div>

        {/* Selected tags overview */}
        {selectedTags.length > 0 && (
          <div className="flex items-center gap-1.5 flex-wrap mt-1">
            <span className="text-xs text-stone-400 font-medium">Wybrane:</span>
            {selectedTags.map((tag) => (
              <span
                key={tag}
                className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium bg-amber-100/60 dark:bg-amber-950/60 text-amber-950 dark:text-amber-200 border border-amber-300/60 dark:border-amber-800/60"
              >
                #{tag}
                <button
                  type="button"
                  onClick={() => handleToggleTag(tag)}
                  className="hover:text-rose-500 cursor-pointer ml-0.5"
                >
                  <X className="h-3 w-3" />
                </button>
              </span>
            ))}
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="flex items-center justify-end gap-3 pt-4 border-t border-stone-100 dark:border-stone-800">
        <button
          type="button"
          onClick={onClose}
          disabled={isSubmitting}
          className="px-4 py-2 text-sm font-medium text-stone-600 dark:text-stone-400 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-xl transition cursor-pointer"
        >
          Anuluj
        </button>
        <button
          type="submit"
          disabled={isSubmitting}
          className="px-5 py-2.5 bg-amber-400 hover:bg-amber-500 active:scale-98 text-amber-950 text-sm font-bold border border-amber-500/40 rounded-xl shadow-xs transition cursor-pointer disabled:opacity-50"
        >
          {isSubmitting
            ? 'Zapisywanie...'
            : choreToEdit
            ? 'Zapisz zmiany'
            : 'Utwórz zadanie'}
        </button>
      </div>
    </form>
  )
}

export function ChoreFormModal({
  isOpen,
  onClose,
  onSubmit,
  choreToEdit,
  existingTags,
  initialValues,
}: ChoreFormModalProps) {
  const [prevIsOpen, setPrevIsOpen] = useState(isOpen)
  const [openCount, setOpenCount] = useState(0)

  if (isOpen !== prevIsOpen) {
    setPrevIsOpen(isOpen)
    if (isOpen) {
      setOpenCount((c) => c + 1)
    }
  }

  const formKey = choreToEdit
    ? `edit-${choreToEdit.id}`
    : `new-${openCount}`

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 bg-black/60 z-40 backdrop-blur-xs animate-in fade-in" />
        <Dialog.Content className="fixed left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2 z-50 w-full max-w-lg bg-white dark:bg-[#14161d] rounded-2xl p-6 shadow-2xl border border-stone-200/90 dark:border-stone-800/90 max-h-[92vh] overflow-y-auto">
          <div className="flex items-center justify-between pb-3.5 border-b border-stone-100 dark:border-stone-800">
            <Dialog.Title className="text-lg font-black tracking-tight text-stone-950 dark:text-white">
              {choreToEdit ? 'Edytuj zadanie' : 'Nowe zadanie domowe'}
            </Dialog.Title>
            <Dialog.Close asChild>
              <button
                type="button"
                className="p-1.5 text-stone-400 hover:text-stone-600 dark:hover:text-stone-200 rounded-lg hover:bg-stone-100 dark:hover:bg-stone-800 transition cursor-pointer"
              >
                <X className="h-4 w-4" />
              </button>
            </Dialog.Close>
          </div>

          <InnerChoreForm
            key={formKey}
            choreToEdit={choreToEdit}
            existingTags={existingTags}
            initialValues={initialValues}
            onClose={onClose}
            onSubmit={onSubmit}
          />
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
