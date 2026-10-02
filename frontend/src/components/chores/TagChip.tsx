import { Tag as TagIcon, X } from 'lucide-react'

export interface TagChipProps {
  tag: string
  isSelected?: boolean
  onClick?: () => void
  onRemove?: () => void
  className?: string
}

export function TagChip({
  tag,
  isSelected = false,
  onClick,
  onRemove,
  className = '',
}: TagChipProps) {
  const baseClasses = `inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-[11px] transition whitespace-nowrap ${
    isSelected
      ? 'bg-amber-100 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300 font-bold shadow-xs'
      : 'bg-stone-100/90 dark:bg-stone-800/80 text-stone-600 dark:text-stone-400 font-medium border border-transparent'
  } ${onClick ? 'hover:bg-stone-200 dark:hover:bg-stone-700 cursor-pointer' : ''} ${className}`

  if (onClick) {
    return (
      <button
        type="button"
        onClick={onClick}
        className={baseClasses}
      >
        <TagIcon className="h-2.5 w-2.5 shrink-0" />
        <span>{tag}</span>
        {onRemove && (
          <span
            role="button"
            tabIndex={0}
            onClick={(e) => {
              e.stopPropagation()
              onRemove()
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.stopPropagation()
                onRemove()
              }
            }}
            className="hover:text-rose-500 cursor-pointer ml-0.5 inline-flex items-center justify-center"
            title="Usuń tag"
          >
            <X className="h-2.5 w-2.5" />
          </span>
        )}
      </button>
    )
  }

  if (onRemove) {
    return (
      <span className={baseClasses}>
        <TagIcon className="h-2.5 w-2.5 shrink-0" />
        <span>{tag}</span>
        <button
          type="button"
          onClick={onRemove}
          className="hover:text-rose-500 cursor-pointer ml-0.5 inline-flex items-center justify-center"
          title="Usuń tag"
        >
          <X className="h-2.5 w-2.5" />
        </button>
      </span>
    )
  }

  return (
    <span className={baseClasses}>
      <TagIcon className="h-2.5 w-2.5 shrink-0" />
      <span>{tag}</span>
    </span>
  )
}
