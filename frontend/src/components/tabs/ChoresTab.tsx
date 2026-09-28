import { useState, useMemo } from 'react'
import {
  Plus,
  ChevronDown,
  ChevronUp,
  AlertCircle,
  RefreshCw,
  Calendar,
  CheckSquare,
  Sparkles,
  Inbox,
} from 'lucide-react'
import {
  useChores,
  useCompleteChore,
  useCreateChore,
  useUpdateChore,
  useDeleteChore,
} from '../../hooks/useChores'
import { ChoreCard } from '../chores/ChoreCard'
import { ChoreFilters } from '../chores/ChoreFilters'
import { ChoreFormModal, type InitialChoreValues } from '../chores/ChoreFormModal'
import { QuickAddChore } from '../chores/QuickAddChore'
import { DeleteChoreDialog } from '../chores/DeleteChoreDialog'
import type { ChoreResponse, CreateChoreRequest, UpdateChoreRequest } from '../../types/chore'

export function ChoresTab() {
  const { data: chores = [], isLoading, isError, error, refetch } = useChores()
  const completeMutation = useCompleteChore()
  const createMutation = useCreateChore()
  const updateMutation = useUpdateChore()
  const deleteMutation = useDeleteChore()

  const [searchQuery, setSearchQuery] = useState('')
  const [selectedTags, setSelectedTags] = useState<string[]>([])
  const [isUnscheduledOpen, setIsUnscheduledOpen] = useState(true)

  // Modal states
  const [isFormModalOpen, setIsFormModalOpen] = useState(false)
  const [choreToEdit, setChoreToEdit] = useState<ChoreResponse | null>(null)
  const [modalInitialValues, setModalInitialValues] = useState<InitialChoreValues | undefined>()
  const [choreToDelete, setChoreToDelete] = useState<ChoreResponse | null>(null)
  const [completingIds, setCompletingIds] = useState<Set<number>>(new Set())

  // Extract all unique tags dynamically
  const availableTags = useMemo(() => {
    const set = new Set<string>()
    chores.forEach((c) => {
      c.tags?.forEach((t) => set.add(t))
    })
    return Array.from(set).sort()
  }, [chores])

  // Filter chores by search query and multi-tag AND logic
  const filteredChores = useMemo(() => {
    const q = searchQuery.trim().toLowerCase()
    return chores.filter((chore) => {
      // 1. Text search on title or description
      const matchesText =
        !q ||
        chore.title.toLowerCase().includes(q) ||
        chore.description?.toLowerCase().includes(q)

      if (!matchesText) return false

      // 2. Multi-tag OR filter: chore must match at least one selected tag
      if (selectedTags.length > 0) {
        const choreTags = chore.tags || []
        const matchesAnyTag = selectedTags.some((t) => choreTags.includes(t))
        if (!matchesAnyTag) return false
      }

      return true
    })
  }, [chores, searchQuery, selectedTags])

  // Split into Scheduled and Unscheduled groups
  const { scheduledChores, unscheduledChores } = useMemo(() => {
    const scheduled: ChoreResponse[] = []
    const unscheduled: ChoreResponse[] = []

    filteredChores.forEach((chore) => {
      if (chore.cadenceDays && chore.cadenceDays > 0) {
        scheduled.push(chore)
      } else {
        unscheduled.push(chore)
      }
    })

    return { scheduledChores: scheduled, unscheduledChores: unscheduled }
  }, [filteredChores])

  const handleToggleTag = (tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    )
  }

  const handleClearTags = () => {
    setSelectedTags([])
  }

  const handleClearAllFilters = () => {
    setSearchQuery('')
    setSelectedTags([])
  }

  const handleCompleteChore = async (choreId: number, points: number) => {
    setCompletingIds((prev) => new Set(prev).add(choreId))
    try {
      await completeMutation.mutateAsync({ choreId, points })
    } catch (err) {
      console.error('Failed to complete chore:', err)
    } finally {
      setCompletingIds((prev) => {
        const next = new Set(prev)
        next.delete(choreId)
        return next
      })
    }
  }

  const handleOpenAdd = (initialValues?: InitialChoreValues) => {
    setChoreToEdit(null)
    setModalInitialValues(initialValues)
    setIsFormModalOpen(true)
  }

  const handleOpenEdit = (chore: ChoreResponse) => {
    setChoreToEdit(chore)
    setIsFormModalOpen(true)
  }

  const handleFormSubmit = async (data: CreateChoreRequest | UpdateChoreRequest) => {
    if (choreToEdit) {
      await updateMutation.mutateAsync({ choreId: choreToEdit.id, req: data })
    } else {
      await createMutation.mutateAsync(data)
    }
  }

  const handleConfirmDelete = async () => {
    if (!choreToDelete) return
    try {
      await deleteMutation.mutateAsync(choreToDelete.id)
      setChoreToDelete(null)
    } catch (err) {
      console.error('Failed to delete chore:', err)
    }
  }

  const activeFiltersCount = (searchQuery ? 1 : 0) + selectedTags.length

  return (
    <div className="flex flex-col gap-4">
      {/* Top Header & Action Bar */}
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-lg font-bold tracking-tight text-slate-900 dark:text-slate-100 flex items-center gap-2">
            <span>Zadania domowe</span>
            {chores.length > 0 && (
              <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
                {chores.length}
              </span>
            )}
          </h2>
        </div>

        <button
          onClick={() => handleOpenAdd()}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-amber-500 hover:bg-amber-600 active:scale-98 text-white rounded-xl text-xs font-semibold shadow-xs transition cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          <span>Dodaj zadanie</span>
        </button>
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="p-12 flex flex-col items-center justify-center gap-3 text-slate-400">
          <RefreshCw className="h-6 w-6 animate-spin text-amber-500" />
          <span className="text-sm font-medium">Ładowanie zadań...</span>
        </div>
      )}

      {/* Error state */}
      {isError && (
        <div className="p-6 rounded-2xl bg-rose-50 dark:bg-rose-950/40 border border-rose-200 dark:border-rose-900 text-rose-800 dark:text-rose-200 flex flex-col items-center gap-3 text-center">
          <AlertCircle className="h-8 w-8 text-rose-500" />
          <p className="text-sm font-medium">
            {error instanceof Error ? error.message : 'Wystąpił błąd podczas ładowania zadań.'}
          </p>
          <button
            onClick={() => refetch()}
            className="px-4 py-1.5 bg-rose-100 dark:bg-rose-900/60 hover:bg-rose-200 dark:hover:bg-rose-900 rounded-lg text-xs font-semibold text-rose-900 dark:text-rose-100 transition cursor-pointer"
          >
            Spróbuj ponownie
          </button>
        </div>
      )}

      {/* Main Content when loaded */}
      {!isLoading && !isError && (
        <>
          {/* Search & Tag Filter Bar */}
          {chores.length > 0 && (
            <ChoreFilters
              searchQuery={searchQuery}
              onSearchChange={setSearchQuery}
              availableTags={availableTags}
              selectedTags={selectedTags}
              onToggleTag={handleToggleTag}
              onClearTags={handleClearTags}
            />
          )}

          {/* Quick Add Chore Form (Always on top of the list) */}
          <QuickAddChore
            onAdd={async (data) => {
              await createMutation.mutateAsync(data)
            }}
            isAdding={createMutation.isPending}
            availableTags={availableTags}
            onOpenFullModal={(values) => handleOpenAdd(values)}
          />

          {/* Empty state: No chores at all in household */}
          {chores.length === 0 && (
            <div className="p-10 bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 text-center flex flex-col items-center gap-3 shadow-xs">
              <div className="p-3 bg-amber-50 dark:bg-amber-950/60 rounded-2xl text-amber-600 dark:text-amber-400">
                <Sparkles className="h-8 w-8" />
              </div>
              <h3 className="text-base font-bold text-slate-900 dark:text-slate-100">
                Brak zadań w Twoim domu
              </h3>
              <p className="text-sm text-slate-500 dark:text-slate-400 max-w-sm">
                Dodaj pierwsze zadanie ze zdefiniowanym cyklem dni lub zadanie jednorazowe, aby zdobywać punkty w rankingu!
              </p>
              <button
                onClick={() => handleOpenAdd()}
                className="mt-2 inline-flex items-center gap-1.5 px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white rounded-xl text-sm font-semibold shadow-xs transition cursor-pointer"
              >
                <Plus className="h-4 w-4" />
                <span>Utwórz pierwsze zadanie</span>
              </button>
            </div>
          )}

          {/* Empty state: Filters applied but 0 matches */}
          {chores.length > 0 && filteredChores.length === 0 && (
            <div className="p-8 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 text-center flex flex-col items-center gap-2 shadow-xs">
              <Inbox className="h-7 w-7 text-slate-400" />
              <p className="text-sm font-semibold text-slate-800 dark:text-slate-200">
                Brak zadań spełniających kryteria wyszukiwania
              </p>
              <p className="text-xs text-slate-500 dark:text-slate-400">
                Spróbuj zmienić zapytanie lub odznaczyć wybrane tagi.
              </p>
              {activeFiltersCount > 0 && (
                <button
                  onClick={handleClearAllFilters}
                  className="mt-2 px-3 py-1.5 text-xs font-semibold text-amber-600 dark:text-amber-400 hover:underline cursor-pointer"
                >
                  Wyczyść filtry ({activeFiltersCount})
                </button>
              )}
            </div>
          )}

          {/* Section 1: Scheduled Chores */}
          {scheduledChores.length > 0 && (
            <div className="flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
                  <Calendar className="h-3.5 w-3.5 text-amber-500" />
                  <span>Zadania cykliczne</span>
                  <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
                    {scheduledChores.length}
                  </span>
                </h3>
              </div>

              <div className="flex flex-col gap-2">
                {scheduledChores.map((chore) => (
                  <ChoreCard
                    key={chore.id}
                    chore={chore}
                    onComplete={handleCompleteChore}
                    isCompleting={completingIds.has(chore.id)}
                    onEdit={handleOpenEdit}
                    onDelete={setChoreToDelete}
                  />
                ))}
              </div>
            </div>
          )}

          {/* Section 2: Unscheduled Chores ("Do zrobienia (bez terminu)") */}
          {unscheduledChores.length > 0 && (
            <div className="flex flex-col gap-2 pt-1">
              <button
                type="button"
                onClick={() => setIsUnscheduledOpen(!isUnscheduledOpen)}
                className="flex items-center justify-between p-1.5 -mx-1.5 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800/60 transition cursor-pointer text-left"
              >
                <div className="flex items-center gap-2">
                  <CheckSquare className="h-4 w-4 text-slate-400" />
                  <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    Do zrobienia (bez terminu)
                  </h3>
                  <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-slate-200 dark:bg-slate-800 text-slate-600 dark:text-slate-400">
                    {unscheduledChores.length}
                  </span>
                </div>
                {isUnscheduledOpen ? (
                  <ChevronUp className="h-4 w-4 text-slate-400" />
                ) : (
                  <ChevronDown className="h-4 w-4 text-slate-400" />
                )}
              </button>

              {isUnscheduledOpen && (
                <div className="flex flex-col gap-2 animate-in fade-in duration-200">
                  {unscheduledChores.map((chore) => (
                    <ChoreCard
                      key={chore.id}
                      chore={chore}
                      onComplete={handleCompleteChore}
                      isCompleting={completingIds.has(chore.id)}
                      onEdit={handleOpenEdit}
                      onDelete={setChoreToDelete}
                    />
                  ))}
                </div>
              )}
            </div>
          )}
        </>
      )}

      {/* Chore Form Modal (Add & Edit) */}
      <ChoreFormModal
        isOpen={isFormModalOpen}
        onClose={() => setIsFormModalOpen(false)}
        onSubmit={handleFormSubmit}
        choreToEdit={choreToEdit}
        existingTags={availableTags}
        initialValues={modalInitialValues}
      />

      {/* Delete Chore Confirmation Dialog */}
      <DeleteChoreDialog
        isOpen={choreToDelete != null}
        chore={choreToDelete}
        onClose={() => setChoreToDelete(null)}
        onConfirm={handleConfirmDelete}
        isDeleting={deleteMutation.isPending}
      />
    </div>
  )
}
