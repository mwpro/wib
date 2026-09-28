import { useState } from 'react'
import { Plus, RefreshCw, SlidersHorizontal } from 'lucide-react'
import type { CreateChoreRequest } from '../../types/chore'
import type { InitialChoreValues } from './ChoreFormModal'

interface QuickAddChoreProps {
  onAdd: (data: CreateChoreRequest) => Promise<void>
  isAdding?: boolean
  availableTags: string[]
  onOpenFullModal?: (initialValues: InitialChoreValues) => void
}

const CADENCE_PRESETS = [
  { label: 'Co tydzień', value: 7 },
  { label: 'Co 2 tyg.', value: 14 },
  { label: 'Co miesiąc', value: 30 },
  { label: 'Bez terminu', value: null },
]

export function QuickAddChore({
  onAdd,
  isAdding = false,
  availableTags,
  onOpenFullModal,
}: QuickAddChoreProps) {
  const [title, setTitle] = useState('')
  const [cadenceDays, setCadenceDays] = useState<number | null>(7)
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [error, setError] = useState<string | null>(null)

  const handleToggleTag = (tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    )
  }

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault()
    const trimmed = title.trim()
    if (!trimmed || isAdding) return

    try {
      setError(null)
      await onAdd({
        title: trimmed,
        cadenceDays,
        points: 1,
        tags: selectedTags.length > 0 ? selectedTags : undefined,
      })
      setTitle('')
      setSelectedTags([])
      setCadenceDays(7)
    } catch (err: unknown) {
      console.error('Failed to quick-add chore:', err)
      setError(err instanceof Error ? err.message : 'Wystąpił błąd podczas dodawania zadania.')
    }
  }

  const handleOpenMore = () => {
    if (error) setError(null)
    if (!onOpenFullModal) return
    onOpenFullModal({
      title,
      cadenceDays,
      tags: selectedTags.length > 0 ? selectedTags : undefined,
    })
    setTitle('')
    setSelectedTags([])
    setCadenceDays(7)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      handleSubmit()
    }
  }

  return (
    <div
      data-testid="quick-add-chore"
      className="bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-3 shadow-[0_2px_8px_rgba(0,0,0,0.03)] flex flex-col gap-2.5 transition-all focus-within:border-amber-400 dark:focus-within:border-amber-600 focus-within:ring-2 focus-within:ring-amber-500/10"
    >
      {/* Error Feedback */}
      {error && (
        <div className="px-3 py-1.5 rounded-xl bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 text-xs border border-rose-200 dark:border-rose-900 flex items-center justify-between">
          <span className="truncate">{error}</span>
          <button
            type="button"
            onClick={() => setError(null)}
            className="text-rose-500 hover:text-rose-700 dark:hover:text-rose-200 ml-2 font-bold cursor-pointer"
            aria-label="Zamknij błąd"
          >
            ×
          </button>
        </div>
      )}

      {/* Row 1: Input + Add Button */}
      <div className="flex items-center gap-2">
        <div className="relative flex-1">
          <Plus className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
          <input
            type="text"
            value={title}
            onChange={(e) => {
              setTitle(e.target.value)
              if (error) setError(null)
            }}
            onKeyDown={handleKeyDown}
            placeholder="Dodaj nowe zadanie (np. Podlać kwiaty)..."
            className="w-full pl-9 pr-3 py-2 bg-stone-50/80 dark:bg-stone-900/60 border border-stone-200 dark:border-stone-700/80 rounded-xl text-sm placeholder:text-stone-400 dark:placeholder:text-stone-500 focus:outline-none focus:bg-white dark:focus:bg-stone-900 focus:border-amber-400 dark:focus:border-amber-500 transition"
          />
        </div>

        <button
          type="button"
          onClick={() => handleSubmit()}
          disabled={isAdding || !title.trim()}
          className="h-9 px-4 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 rounded-xl text-xs font-bold border border-amber-500/30 transition cursor-pointer shadow-xs disabled:opacity-40 disabled:cursor-not-allowed shrink-0 flex items-center justify-center gap-1.5"
        >
          {isAdding ? (
            <RefreshCw className="h-3.5 w-3.5 animate-spin" />
          ) : (
            <span>Dodaj</span>
          )}
        </button>
      </div>

      {/* Row 2: Condensed Cadence & Tags Selectors */}
      <div className="flex items-center justify-between gap-2 text-xs text-stone-500 dark:text-stone-400 flex-wrap">
        {/* Quick Cadence Pills */}
        <div className="flex items-center gap-1 flex-wrap">
          <span className="text-[10px] uppercase tracking-wider text-stone-400 font-semibold mr-0.5">Cykl:</span>
          {CADENCE_PRESETS.map((preset) => {
            const isSelected = cadenceDays === preset.value
            return (
              <button
                key={preset.label}
                type="button"
                onClick={() => setCadenceDays(preset.value)}
                className={`px-2 py-0.5 rounded-lg text-[11px] transition cursor-pointer ${
                  isSelected
                    ? 'bg-amber-100 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300 font-bold shadow-xs'
                    : 'bg-stone-100/90 dark:bg-stone-800/80 hover:bg-stone-200 dark:hover:bg-stone-700 text-stone-600 dark:text-stone-400 font-medium'
                }`}
              >
                {preset.label}
              </button>
            )
          })}
        </div>

        {/* Tags and More options */}
        <div className="flex items-center gap-2 ml-auto flex-wrap">
          {availableTags.length > 0 && (
            <div className="flex items-center gap-1 flex-wrap">
              <span className="text-[10px] uppercase tracking-wider text-stone-400 font-semibold mr-0.5">Tagi:</span>
              {availableTags.slice(0, 3).map((tag) => {
                const isSelected = selectedTags.includes(tag)
                return (
                  <button
                    key={tag}
                    type="button"
                    onClick={() => handleToggleTag(tag)}
                    className={`px-1.5 py-0.5 rounded-md text-[10px] font-medium transition cursor-pointer ${
                      isSelected
                        ? 'bg-amber-100 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300/80 font-bold'
                        : 'bg-stone-100 dark:bg-stone-800 hover:bg-stone-200 dark:hover:bg-stone-700 text-stone-600 dark:text-stone-400'
                    }`}
                  >
                    #{tag}
                  </button>
                )
              })}
            </div>
          )}

          {/* More options button (opens full modal) */}
          {onOpenFullModal && (
            <button
              type="button"
              onClick={handleOpenMore}
              title="Więcej opcji (opis, własne dni)"
              className="inline-flex items-center gap-1 text-[11px] text-stone-500 hover:text-amber-800 dark:hover:text-amber-300 font-medium transition cursor-pointer ml-1"
            >
              <SlidersHorizontal className="h-3 w-3" />
              <span>Więcej</span>
            </button>
          )}
        </div>
      </div>
    </div>
  )
}
