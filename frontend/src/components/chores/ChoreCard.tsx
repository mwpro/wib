import { useState, useRef, useEffect } from 'react'
import { Check, Clock, MoreVertical, Edit2, Trash2 } from 'lucide-react'
import { ChoreBadge } from './ChoreBadge'
import type { ChoreResponse, FreshnessUrgency } from '../../types/chore'

interface ChoreCardProps {
  chore: ChoreResponse
  onComplete: (choreId: number, points: number) => void
  isCompleting?: boolean
  onEdit: (chore: ChoreResponse) => void
  onDelete: (chore: ChoreResponse) => void
}

const BORDER_ACCENT: Record<FreshnessUrgency, string> = {
  Fresh: 'border-l-emerald-500',
  DueSoon: 'border-l-amber-500',
  Overdue: 'border-l-orange-500',
  Neglected: 'border-l-rose-500',
  Unscheduled: 'border-l-slate-400',
}

export function ChoreCard({
  chore,
  onComplete,
  isCompleting = false,
  onEdit,
  onDelete,
}: ChoreCardProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [isDone, setIsDone] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false)
      }
    }
    if (menuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [menuOpen])

  const handleComplete = () => {
    if (isCompleting || isDone) return
    setIsDone(true)
    onComplete(chore.id, chore.points)
  }

  // Formatting cadence text
  const cadenceText = chore.cadenceDays
    ? `co ${chore.cadenceDays} ${chore.cadenceDays === 1 ? 'dzień' : 'dni'}`
    : 'bez terminu'

  // Formatting completion text
  let lastDoneText = 'nigdy nierobione'
  if (chore.lastCompletedAt) {
    const days = chore.daysSinceLastDone ?? 0
    if (days === 0) {
      lastDoneText = 'zrobione dzisiaj'
    } else if (days === 1) {
      lastDoneText = 'zrobione wczoraj'
    } else {
      lastDoneText = `zrobione ${days} dni temu`
    }
  }

  const borderClass = BORDER_ACCENT[chore.urgency] || BORDER_ACCENT.Unscheduled

  return (
    <div
      data-testid="chore-card"
      className={`relative bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 shadow-sm border-l-4 ${borderClass} flex flex-col gap-3 transition-all`}
    >
      {/* Top Header Row */}
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap mb-1">
            <ChoreBadge urgency={chore.urgency} />
          </div>
          <h3 className="text-base font-bold text-slate-900 dark:text-slate-100 leading-snug break-words">
            {chore.title}
          </h3>
        </div>

        {/* 3-dots Menu Button */}
        <div className="relative" ref={menuRef}>
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            title="Opcje zadania"
            className="p-1.5 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 transition cursor-pointer"
          >
            <MoreVertical className="h-4 w-4" />
          </button>

          {menuOpen && (
            <div className="absolute right-0 top-8 z-20 w-36 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-lg py-1 text-sm">
              <button
                onClick={() => {
                  setMenuOpen(false)
                  onEdit(chore)
                }}
                className="w-full px-3 py-2 text-left flex items-center gap-2 text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-700 transition cursor-pointer"
              >
                <Edit2 className="h-3.5 w-3.5" />
                <span>Edytuj</span>
              </button>
              <button
                onClick={() => {
                  setMenuOpen(false)
                  onDelete(chore)
                }}
                className="w-full px-3 py-2 text-left flex items-center gap-2 text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition cursor-pointer"
              >
                <Trash2 className="h-3.5 w-3.5" />
                <span>Usuń</span>
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Description if present */}
      {chore.description && (
        <p className="text-sm text-slate-600 dark:text-slate-400 whitespace-pre-line break-words">
          {chore.description}
        </p>
      )}

      {/* Cadence & Freshness details */}
      <div className="flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400 flex-wrap">
        <span className="inline-flex items-center gap-1 font-medium">
          <Clock className="h-3.5 w-3.5 text-slate-400" />
          {cadenceText}
        </span>
        <span>•</span>
        <span>{lastDoneText}</span>
      </div>

      {/* Tags chips */}
      {chore.tags && chore.tags.length > 0 && (
        <div className="flex items-center gap-1.5 flex-wrap">
          {chore.tags.map((tag) => (
            <span
              key={tag}
              className="px-2 py-0.5 rounded-md text-xs font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300"
            >
              #{tag}
            </span>
          ))}
        </div>
      )}

      {/* 1-Tap "Zrobione!" Button */}
      <div className="pt-1">
        <button
          onClick={handleComplete}
          disabled={isCompleting || isDone}
          className={`w-full py-3 px-4 rounded-xl font-bold text-sm flex items-center justify-center gap-2 shadow-xs transition-all duration-200 ${
            isDone
              ? 'bg-emerald-100 dark:bg-emerald-950/70 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800 cursor-default'
              : 'bg-emerald-600 hover:bg-emerald-700 active:scale-[0.98] text-white cursor-pointer disabled:opacity-50'
          }`}
        >
          <Check className="h-4 w-4 stroke-[3]" />
          <span>{isDone ? `Ukończono (+${chore.points} pkt)` : 'Zrobione!'}</span>
        </button>
      </div>
    </div>
  )
}
