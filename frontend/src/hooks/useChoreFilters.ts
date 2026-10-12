import { useState, useEffect } from 'react'
import { parseCadenceFilter, type CadenceFilter } from '../utils/cadence'

interface ChoreFiltersState {
  searchQuery: string
  selectedTags: string[]
  cadence: CadenceFilter | null
}

function readFromUrl(): ChoreFiltersState {
  const params = new URLSearchParams(window.location.search)
  return {
    searchQuery: params.get('q') ?? '',
    selectedTags: params.getAll('tag'),
    cadence: parseCadenceFilter(params.get('cadence')),
  }
}

function toSearchString({ searchQuery, selectedTags, cadence }: ChoreFiltersState): string {
  const params = new URLSearchParams()
  if (searchQuery) params.set('q', searchQuery)
  selectedTags.forEach((tag) => params.append('tag', tag))
  if (cadence != null) params.set('cadence', String(cadence))
  const search = params.toString()
  return search ? `?${search}` : ''
}

/**
 * Chore backlog filters persisted in the URL query string, so they survive a refresh.
 * Uses replaceState so typing does not flood the browser history.
 */
export function useChoreFilters() {
  const [filters, setFilters] = useState<ChoreFiltersState>(readFromUrl)

  useEffect(() => {
    const search = toSearchString(filters)
    if (window.location.search !== search) {
      window.history.replaceState(null, '', `${window.location.pathname}${search}`)
    }
  }, [filters])

  return {
    ...filters,
    setSearchQuery: (searchQuery: string) => setFilters((f) => ({ ...f, searchQuery })),
    setSelectedTags: (selectedTags: string[]) => setFilters((f) => ({ ...f, selectedTags })),
    setCadence: (cadence: CadenceFilter | null) => setFilters((f) => ({ ...f, cadence })),
    clearAll: () => setFilters({ searchQuery: '', selectedTags: [], cadence: null }),
  }
}
