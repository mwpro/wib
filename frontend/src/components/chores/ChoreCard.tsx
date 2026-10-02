import { useState, useRef, useEffect } from 'react'
import { Check, Clock, ChevronDown, Edit2, Trash2 } from 'lucide-react'
import { ChoreBadge } from './ChoreBadge'
import { TagChip } from './TagChip'
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
  { border: string }
> = {
  Fresh: {
    border: 'border-l-emerald-500',
  },
  DueSoon: {
    border: 'border-l-amber-500',
  },
  Overdue: {
    border: 'border-l-orange-500',
  },
  Neglected: {
    border: 'border-l-rose-500',
  },
  Unscheduled: {
    border: 'border-l-stone-300 dark:border-l-stone-600',
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
      className={`group relative bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl px-4 py-3 border-l-4 ${urgencyStyle.border} shadow-[0_1px_3px_rgba(0,0,0,0.03)] hover:border-amber-300 dark:hover:border-amber-700/60 flex flex-col gap-2 transition-all`}
    >
      {/* Row 1: Title (left) & Button + Menu (right) */}
      <div className="flex items-center justify-between gap-3">
        <h3 className="text-[14px] font-semibold text-stone-900 dark:text-stone-50 leading-snug tracking-tight break-words flex-1 min-w-0">
          {chore.title}
        </h3>

        {/* Right Actions: Split Button with Dropdown */}
        <div className="relative inline-flex rounded-xl shadow-xs shrink-0" ref={menuRef}>
          {/* Main 1-tap Complete Button */}
          <button
            type="button"
            onClick={handleComplete}
            disabled={isCompleting || isDone}
            aria-label={isDone ? `Ukończono (+${chore.points} pkt)` : 'Zrobione!'}
            className={`h-8 w-[92px] rounded-l-xl font-bold text-xs flex items-center justify-center gap-1.5 transition-all duration-150 ${
              isDone
                ? 'bg-stone-100 dark:bg-stone-800 text-emerald-800 dark:text-emerald-400 border border-stone-200 dark:border-stone-700 border-r-0 cursor-default font-semibold'
                : 'bg-emerald-600 hover:bg-emerald-500 active:scale-[0.98] text-white cursor-pointer shadow-xs disabled:opacity-50'
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
            className={`h-8 px-2 rounded-r-xl flex items-center justify-center transition-all cursor-pointer ${
              isDone
                ? 'bg-stone-100 dark:bg-stone-800 text-stone-400 dark:text-stone-500 border border-stone-200 dark:border-stone-700 border-l-stone-200 dark:border-l-stone-700 hover:bg-stone-200 dark:hover:bg-stone-700 hover:text-stone-700'
                : 'bg-emerald-600 hover:bg-emerald-500 text-white border-l border-emerald-700/40'
            }`}
          >
            <ChevronDown
              className={`h-3.5 w-3.5 transition-transform duration-200 ${menuOpen ? 'rotate-180' : ''}`}
            />
          </button>

          {/* Dropdown Menu */}
          {menuOpen && (
            <div className="absolute right-0 top-9.5 z-20 w-36 bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 rounded-xl shadow-xl py-1 text-xs animate-in fade-in zoom-in-95 duration-100">
              <button
                type="button"
                onClick={() => {
                  setMenuOpen(false)
                  onEdit(chore)
                }}
                className="w-full px-3 py-2 text-left flex items-center gap-2 text-stone-700 dark:text-stone-200 hover:bg-stone-100 dark:hover:bg-stone-800 font-medium transition cursor-pointer"
              >
                <Edit2 className="h-3.5 w-3.5 text-stone-400" />
                <span>Edytuj</span>
              </button>
              <button
                type="button"
                onClick={() => {
                  setMenuOpen(false)
                  onDelete(chore)
                }}
                className="w-full px-3 py-2 text-left flex items-center gap-2 text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-950/40 font-medium transition cursor-pointer"
              >
                <Trash2 className="h-3.5 w-3.5" />
                <span>Usuń</span>
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Row 2: Metadata taking full width of card */}
      <div className="flex items-center gap-2 text-xs text-stone-500 dark:text-stone-400 flex-wrap min-w-0">
        <ChoreBadge urgency={chore.urgency} />
        <span className="inline-flex items-center gap-1 text-[11px] text-stone-600 dark:text-stone-400 font-medium">
          <Clock className="h-3 w-3 text-stone-400" />
          {cadenceText}
        </span>
        <span className="text-stone-300 dark:text-stone-700 select-none">/</span>
        <span className="text-[11px] text-stone-500 dark:text-stone-400">{lastDoneText}</span>
      </div>

      {/* Row 3: Tags on their own dedicated line */}
      {chore.tags && chore.tags.length > 0 && (
        <div className="flex items-center gap-1.5 flex-wrap pt-0.5">
          {chore.tags.map((tag) => (
            <TagChip key={tag} tag={tag} />
          ))}
        </div>
      )}

      {/* Row 4: Description under tags */}
      {chore.description && (
        <div className="text-xs text-stone-500 dark:text-stone-400 pt-0.5">
          <div
            onClick={() => isExpandable && setIsExpanded(!isExpanded)}
            className={`flex items-start justify-between gap-2 ${
              isExpandable ? 'cursor-pointer group/desc' : ''
            }`}
          >
            <p
              className={`break-words flex-1 leading-relaxed ${
                isExpanded
                  ? 'whitespace-pre-line text-stone-800 dark:text-stone-200'
                  : 'line-clamp-1 text-stone-500 dark:text-stone-400'
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
                className="text-[11px] text-amber-800 dark:text-amber-400 font-semibold hover:underline shrink-0 cursor-pointer pt-0.5"
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
