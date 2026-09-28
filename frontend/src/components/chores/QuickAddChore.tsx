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
      await onAdd({
        title: trimmed,
        cadenceDays,
        points: 1,
        tags: selectedTags.length > 0 ? selectedTags : undefined,
      })
      setTitle('')
      setSelectedTags([])
      setCadenceDays(7)
    } catch (err) {
      console.error('Failed to quick-add chore:', err)
    }
  }

  const handleOpenMore = () => {
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
      className="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-2.5 shadow-xs flex flex-col gap-2 transition-all focus-within:border-amber-500/50 focus-within:ring-2 focus-within:ring-amber-500/10"
    >
      {/* Row 1: Input + Add Button */}
      <div className="flex items-center gap-2">
        <div className="relative flex-1">
          <Plus className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Dodaj nowe zadanie (np. Podlać kwiaty)..."
            className="w-full pl-9 pr-3 py-1.5 bg-slate-50 dark:bg-slate-800/60 border border-slate-200/80 dark:border-slate-700/80 rounded-xl text-sm placeholder:text-slate-400 dark:placeholder:text-slate-500 focus:outline-none focus:bg-white dark:focus:bg-slate-800 focus:border-amber-500 transition"
          />
        </div>

        <button
          type="button"
          onClick={() => handleSubmit()}
          disabled={isAdding || !title.trim()}
          className="h-8.5 px-3.5 bg-amber-500 hover:bg-amber-600 active:scale-95 text-white rounded-xl text-xs font-semibold shadow-xs transition cursor-pointer disabled:opacity-40 disabled:cursor-not-allowed shrink-0 flex items-center justify-center gap-1"
        >
          {isAdding ? (
            <RefreshCw className="h-3.5 w-3.5 animate-spin" />
          ) : (
            <span>Dodaj</span>
          )}
        </button>
      </div>

      {/* Row 2: Condensed Cadence & Tags Selectors */}
      <div className="flex items-center justify-between gap-2 text-xs text-slate-500 dark:text-slate-400 flex-wrap">
        {/* Quick Cadence Pills */}
        <div className="flex items-center gap-1 flex-wrap">
          <span className="text-[11px] font-medium text-slate-400 mr-0.5">Cykl:</span>
          {CADENCE_PRESETS.map((preset) => {
            const isSelected = cadenceDays === preset.value
            return (
              <button
                key={preset.label}
                type="button"
                onClick={() => setCadenceDays(preset.value)}
                className={`px-2 py-0.5 rounded-md text-[11px] font-medium transition cursor-pointer ${
                  isSelected
                    ? 'bg-amber-100 dark:bg-amber-950/70 text-amber-800 dark:text-amber-300 border border-amber-300 dark:border-amber-800 font-semibold'
                    : 'bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-400'
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
              <span className="text-[11px] font-medium text-slate-400 mr-0.5">Tagi:</span>
              {availableTags.slice(0, 3).map((tag) => {
                const isSelected = selectedTags.includes(tag)
                return (
                  <button
                    key={tag}
                    type="button"
                    onClick={() => handleToggleTag(tag)}
                    className={`px-1.5 py-0.5 rounded-md text-[10px] font-medium transition cursor-pointer ${
                      isSelected
                        ? 'bg-amber-500 text-white font-semibold'
                        : 'bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-400'
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
              className="inline-flex items-center gap-1 text-[11px] text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition cursor-pointer"
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
