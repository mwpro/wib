import { useState } from 'react'
import { Plus, AlertCircle, RefreshCw, CheckSquare, Inbox } from 'lucide-react'
import {
  useChores,
  useCompleteChore,
  useCreateChore,
  useUpdateChore,
  useDeleteChore,
} from '../../hooks/useChores'
import { useBacklogView } from '../../hooks/useBacklogView'
import { useConfirmedAction } from '../../hooks/useConfirmedAction'
import { ChoreCard } from '../chores/ChoreCard'
import { ChoreFilters } from '../chores/ChoreFilters'
import { ChoreFormModal, type InitialChoreValues } from '../chores/ChoreFormModal'
import { DeleteChoreDialog } from '../chores/DeleteChoreDialog'
import { RepeatCompletionDialog } from '../chores/RepeatCompletionDialog'
import { ChoreHistoryModal } from '../chores/ChoreHistoryModal'
import type { ChoreResponse, CreateChoreRequest, UpdateChoreRequest } from '../../types/chore'

export function ChoresTab() {
  const { data: chores = [], isLoading, isError, error, refetch } = useChores()
  const completeMutation = useCompleteChore()
  const createMutation = useCreateChore()
  const updateMutation = useUpdateChore()
  const deleteMutation = useDeleteChore()

  const backlog = useBacklogView(chores)
  const deleteChore = useConfirmedAction((chore: ChoreResponse) =>
    deleteMutation.mutateAsync(chore.id)
  )
  const repeatCompletion = useConfirmedAction((chore: ChoreResponse) =>
    completeMutation.mutateAsync({ choreId: chore.id, points: chore.points })
  )

  // Modal states
  const [isFormModalOpen, setIsFormModalOpen] = useState(false)
  const [choreToEdit, setChoreToEdit] = useState<ChoreResponse | null>(null)
  const [modalInitialValues, setModalInitialValues] = useState<InitialChoreValues | undefined>()
  const [choreForHistory, setChoreForHistory] = useState<ChoreResponse | null>(null)
  const [completingIds, setCompletingIds] = useState<Set<number>>(new Set())

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

  return (
    <div className="flex flex-col gap-5">
      {/* Top Header & Action Bar */}
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-xl font-black tracking-tight text-stone-900 dark:text-white flex items-center gap-2.5">
            <span>Zadania domowe</span>
            {chores.length > 0 && (
              <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-600 dark:text-stone-400 border border-stone-200/80 dark:border-stone-700/80">
                {chores.length}
              </span>
            )}
          </h2>
        </div>

        <button
          type="button"
          onClick={() => handleOpenAdd()}
          className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 rounded-xl text-xs font-bold border border-amber-500/40 shadow-xs transition cursor-pointer"
        >
          <Plus className="h-4 w-4" />
          <span>Dodaj zadanie</span>
        </button>
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="p-12 flex flex-col items-center justify-center gap-3 text-stone-400">
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
            type="button"
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
              searchQuery={backlog.searchQuery}
              onSearchChange={backlog.setSearchQuery}
              availableCadences={backlog.availableCadences}
              selectedCadence={backlog.cadence}
              onSelectCadence={backlog.setCadence}
              availableTags={backlog.availableTags}
              selectedTags={backlog.selectedTags}
              onToggleTag={backlog.toggleTag}
              onClearTags={backlog.clearTags}
            />
          )}

          {/* Empty state: No chores at all in household */}
          {chores.length === 0 && (
            <div className="p-10 bg-white dark:bg-[#14161d] rounded-2xl border border-stone-200/90 dark:border-stone-800/80 text-center flex flex-col items-center gap-3 shadow-[0_2px_8px_rgba(0,0,0,0.02)]">
              <div className="w-12 h-12 rounded-2xl bg-amber-100 dark:bg-amber-950/80 text-amber-800 dark:text-amber-400 flex items-center justify-center border border-amber-300 dark:border-amber-800">
                <CheckSquare className="h-6 w-6 text-amber-700 dark:text-amber-400" />
              </div>
              <h3 className="text-base font-bold text-stone-900 dark:text-stone-100 tracking-tight">
                Brak zadań w Twoim domu
              </h3>
              <p className="text-sm text-stone-500 dark:text-stone-400 max-w-sm leading-relaxed">
                Dodaj pierwsze zadanie ze zdefiniowanym cyklem dni lub zadanie jednorazowe, aby zdobywać punkty w rankingu!
              </p>
              <button
                type="button"
                onClick={() => handleOpenAdd()}
                className="mt-2 inline-flex items-center gap-1.5 px-4 py-2 bg-amber-400 hover:bg-amber-500 active:scale-95 text-amber-950 rounded-xl text-xs font-bold border border-amber-500/40 shadow-xs transition cursor-pointer"
              >
                <Plus className="h-4 w-4" />
                <span>Utwórz pierwsze zadanie</span>
              </button>
            </div>
          )}

          {/* Empty state: Filters applied but 0 matches */}
          {chores.length > 0 && backlog.visibleChores.length === 0 && (
            <div className="p-8 bg-white dark:bg-[#14161d] rounded-2xl border border-stone-200/90 dark:border-stone-800/80 text-center flex flex-col items-center gap-2 shadow-[0_2px_8px_rgba(0,0,0,0.02)]">
              <Inbox className="h-7 w-7 text-stone-400" />
              <p className="text-sm font-semibold text-stone-800 dark:text-stone-200 tracking-tight">
                Brak zadań spełniających kryteria wyszukiwania
              </p>
              <p className="text-xs text-stone-500 dark:text-stone-400">
                Spróbuj zmienić zapytanie lub odznaczyć wybrane filtry.
              </p>
              {backlog.activeFiltersCount > 0 && (
                <button
                  type="button"
                  onClick={backlog.clearAll}
                  className="mt-2 px-3 py-1.5 text-xs font-semibold text-amber-800 dark:text-amber-400 hover:underline cursor-pointer"
                >
                  Wyczyść filtry ({backlog.activeFiltersCount})
                </button>
              )}
            </div>
          )}

          {/* Backlog */}
          {backlog.visibleChores.length > 0 && (
            <div className="flex flex-col gap-2">
              {backlog.visibleChores.map((chore) => (
                <ChoreCard
                  key={chore.id}
                  chore={chore}
                  onComplete={handleCompleteChore}
                  isCompleting={completingIds.has(chore.id)}
                  onEdit={handleOpenEdit}
                  onDelete={deleteChore.open}
                  onHistory={setChoreForHistory}
                  onRepeat={repeatCompletion.open}
                />
              ))}
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
        existingTags={backlog.availableTags}
        initialValues={modalInitialValues}
      />

      {/* Delete Chore Confirmation Dialog */}
      <DeleteChoreDialog {...deleteChore.dialog} chore={deleteChore.target} />

      {/* Same-day Repeat Confirmation Dialog */}
      <RepeatCompletionDialog {...repeatCompletion.dialog} chore={repeatCompletion.target} />

      <ChoreHistoryModal chore={choreForHistory} onClose={() => setChoreForHistory(null)} />
    </div>
  )
}
