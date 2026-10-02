import { Search, X, Tag as TagIcon } from 'lucide-react'

interface ChoreFiltersProps {
  searchQuery: string
  onSearchChange: (query: string) => void
  availableTags: string[]
  selectedTags: string[]
  onToggleTag: (tag: string) => void
  onClearTags: () => void
}

export function ChoreFilters({
  searchQuery,
  onSearchChange,
  availableTags,
  selectedTags,
  onToggleTag,
  onClearTags,
}: ChoreFiltersProps) {
  return (
    <div data-testid="chore-filters" className="flex flex-col gap-2.5">
      {/* Search Input */}
      <div className="relative">
        <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-stone-400" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Szukaj zadań..."
          className="w-full pl-10 pr-9 py-2 bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-xl text-sm placeholder:text-stone-400 dark:placeholder:text-stone-500 focus:outline-none focus:ring-2 focus:ring-amber-500/10 focus:border-amber-400 dark:focus:border-amber-500 shadow-2xs transition"
        />
        {searchQuery && (
          <button
            type="button"
            onClick={() => onSearchChange('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-stone-400 hover:text-stone-600 dark:hover:text-stone-200 rounded-md transition"
            title="Wyczyść wyszukiwanie"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      {/* Tag Pills */}
      {availableTags.length > 0 && (
        <div className="flex items-center gap-1.5 overflow-x-auto pb-0.5 scrollbar-none text-xs">
          <button
            type="button"
            onClick={onClearTags}
            className={`px-2.5 py-1 rounded-lg text-[11px] transition whitespace-nowrap cursor-pointer ${
              selectedTags.length === 0
                ? 'bg-amber-100 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300 font-bold shadow-xs'
                : 'bg-stone-100/90 dark:bg-stone-800/80 text-stone-600 dark:text-stone-400 hover:bg-stone-200 dark:hover:bg-stone-700 font-medium'
            }`}
          >
            Wszystkie
          </button>
          {availableTags.map((tag) => {
            const isSelected = selectedTags.includes(tag)
            return (
              <button
                type="button"
                key={tag}
                onClick={() => onToggleTag(tag)}
                className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-[11px] transition whitespace-nowrap cursor-pointer ${
                  isSelected
                    ? 'bg-amber-100 dark:bg-amber-950/70 text-amber-950 dark:text-amber-300 border border-amber-300 font-bold shadow-xs'
                    : 'bg-stone-100/90 dark:bg-stone-800/80 text-stone-600 dark:text-stone-400 hover:bg-stone-200 dark:hover:bg-stone-700 font-medium'
                }`}
              >
                <TagIcon className="h-2.5 w-2.5" />
                <span>{tag}</span>
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}
