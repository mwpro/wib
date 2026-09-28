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
    <div className="flex flex-col gap-3">
      {/* Search Input */}
      <div className="relative">
        <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
        <input
          type="text"
          value={searchQuery}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Szukaj zadań..."
          className="w-full pl-10 pr-9 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl text-sm placeholder:text-slate-400 dark:placeholder:text-slate-500 focus:outline-none focus:ring-2 focus:ring-amber-500/20 focus:border-amber-500 transition"
        />
        {searchQuery && (
          <button
            onClick={() => onSearchChange('')}
            className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 rounded-md transition"
            title="Wyczyść wyszukiwanie"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      {/* Tag Pills */}
      {availableTags.length > 0 && (
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none text-xs">
          <button
            onClick={onClearTags}
            className={`px-3 py-1.5 rounded-lg font-medium transition whitespace-nowrap cursor-pointer ${
              selectedTags.length === 0
                ? 'bg-amber-500 text-white shadow-sm'
                : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'
            }`}
          >
            Wszystkie
          </button>
          {availableTags.map((tag) => {
            const isSelected = selectedTags.includes(tag)
            return (
              <button
                key={tag}
                onClick={() => onToggleTag(tag)}
                className={`inline-flex items-center gap-1 px-3 py-1.5 rounded-lg font-medium transition whitespace-nowrap cursor-pointer ${
                  isSelected
                    ? 'bg-amber-500 text-white shadow-sm'
                    : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'
                }`}
              >
                <TagIcon className="h-3 w-3" />
                <span>#{tag}</span>
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}
