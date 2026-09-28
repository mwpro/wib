import { useState, useRef, useEffect } from 'react'
import { Check, Clock, ChevronDown, Edit2, Trash2 } from 'lucide-react'
import { ChoreBadge } from './ChoreBadge'
import type { ChoreResponse, FreshnessUrgency } from '../../types/chore'

interface ChoreCardProps {
  chore: ChoreResponse
  onComplete: (choreId: number, points: number) => void
  isCompleting?: boolean
  onEdit: (chore: ChoreResponse) => void
  onDelete: (chore: ChoreResponse) => void
}

const URGENCY_STYLES: Record<
  FreshnessUrgency,
  { border: string; shadow: string }
> = {
  Fresh: {
    border: 'border-l-emerald-500 dark:border-l-emerald-400',
    shadow: 'shadow-sm shadow-emerald-500/20 dark:shadow-emerald-950/50',
  },
  DueSoon: {
    border: 'border-l-amber-500 dark:border-l-amber-400',
    shadow: 'shadow-sm shadow-amber-500/20 dark:shadow-amber-950/50',
  },
  Overdue: {
    border: 'border-l-orange-500 dark:border-l-orange-400',
    shadow: 'shadow-sm shadow-orange-500/25 dark:shadow-orange-950/60',
  },
  Neglected: {
    border: 'border-l-rose-500 dark:border-l-rose-400',
    shadow: 'shadow-sm shadow-rose-500/30 dark:shadow-rose-950/70',
  },
  Unscheduled: {
    border: 'border-l-slate-300 dark:border-l-slate-600',
    shadow: 'shadow-xs',
  },
}

export function ChoreCard({
  chore,
  onComplete,
  isCompleting = false,
  onEdit,
  onDelete,
}: ChoreCardProps) {
  const [menuOpen, setMenuOpen] = useState(false)
  const isDone = Boolean(chore.lastCompletedAt) && chore.daysSinceLastDone === 0
  const [isExpanded, setIsExpanded] = useState(false)
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

  const urgencyStyle = URGENCY_STYLES[chore.urgency] || URGENCY_STYLES.Unscheduled

  // Description first-line extraction and expandability check
  const descriptionLines = chore.description ? chore.description.split(/\r?\n/) : []
  const firstDescLine = descriptionLines[0] ?? ''
  const isExpandable =
    descriptionLines.length > 1 || (chore.description ? chore.description.length > 60 : false)

  return (
    <div
      data-testid="chore-card"
      className={`relative bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl px-3.5 py-2.5 border-l-4 ${urgencyStyle.border} ${urgencyStyle.shadow} flex flex-col gap-1.5 transition-all`}
    >
      {/* Row 1: Title (left) & Button + Menu (right) */}
      <div className="flex items-center justify-between gap-2.5">
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100 leading-snug break-words flex-1 min-w-0">
          {chore.title}
        </h3>

        {/* Right Actions: Split Button with Dropdown */}
        <div className="relative inline-flex rounded-lg shadow-xs shrink-0" ref={menuRef}>
          {/* Main 1-tap Complete Button */}
          <button
            type="button"
            onClick={handleComplete}
            disabled={isCompleting || isDone}
            aria-label={isDone ? `Ukończono (+${chore.points} pkt)` : 'Zrobione!'}
            className={`h-8 w-[88px] rounded-l-lg font-bold text-xs flex items-center justify-center gap-1 transition-all duration-200 ${
              isDone
                ? 'bg-emerald-100 dark:bg-emerald-950/70 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800 border-r-0 cursor-default'
                : 'bg-emerald-600 hover:bg-emerald-700 active:scale-95 text-white cursor-pointer disabled:opacity-50'
            }`}
          >
            <Check className="h-3.5 w-3.5 stroke-[3]" />
            <span className="whitespace-nowrap">{isDone ? `+${chore.points} pkt` : 'Zrobione!'}</span>
          </button>

          {/* Chevron Dropdown Trigger */}
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation()
              setMenuOpen(!menuOpen)
            }}
            title="Więcej opcji"
            className={`h-8 px-1.5 rounded-r-lg flex items-center justify-center transition-all cursor-pointer ${
              isDone
                ? 'bg-emerald-100 dark:bg-emerald-950/70 text-emerald-800 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800 border-l-emerald-200 dark:border-l-emerald-800 hover:bg-emerald-200 dark:hover:bg-emerald-900'
                : 'bg-emerald-600 hover:bg-emerald-700 text-white border-l border-emerald-700/60'
            }`}
          >
            <ChevronDown
              className={`h-3.5 w-3.5 transition-transform duration-200 ${menuOpen ? 'rotate-180' : ''}`}
            />
          </button>

          {/* Dropdown Menu */}
          {menuOpen && (
            <div className="absolute right-0 top-9 z-20 w-36 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-lg py-1 text-sm animate-in fade-in zoom-in-95 duration-100">
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

      {/* Row 2: Metadata taking full width of card */}
      <div className="flex items-center gap-1.5 text-xs text-slate-500 dark:text-slate-400 flex-wrap min-w-0">
        <ChoreBadge urgency={chore.urgency} />
        <span className="inline-flex items-center gap-1 font-medium text-[11px]">
          <Clock className="h-3 w-3 text-slate-400" />
          {cadenceText}
        </span>
        <span>•</span>
        <span className="text-[11px]">{lastDoneText}</span>
      </div>

      {/* Row 3: Tags on their own dedicated line */}
      {chore.tags && chore.tags.length > 0 && (
        <div className="flex items-center gap-1 flex-wrap">
          {chore.tags.map((tag) => (
            <span
              key={tag}
              className="px-1.5 py-0.5 rounded-md text-[11px] font-medium bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300"
            >
              #{tag}
            </span>
          ))}
        </div>
      )}

      {/* Row 4: Description under tags (1st line always shown, expandable only when needed) */}
      {chore.description && (
        <div className="text-xs text-slate-500 dark:text-slate-400 pt-0.5">
          <div
            onClick={() => isExpandable && setIsExpanded(!isExpanded)}
            className={`flex items-start justify-between gap-2 ${
              isExpandable ? 'cursor-pointer group' : ''
            }`}
          >
            <p
              className={`break-words flex-1 leading-relaxed ${
                isExpanded
                  ? 'whitespace-pre-line text-slate-700 dark:text-slate-300'
                  : 'line-clamp-1 text-slate-500 dark:text-slate-400'
              }`}
            >
              {isExpanded ? chore.description : firstDescLine}
            </p>
            {isExpandable && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation()
                  setIsExpanded(!isExpanded)
                }}
                className="text-[11px] text-amber-600 dark:text-amber-400 font-medium hover:underline shrink-0 cursor-pointer pt-0.5"
              >
                {isExpanded ? 'zwiń' : 'więcej'}
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  )
}
