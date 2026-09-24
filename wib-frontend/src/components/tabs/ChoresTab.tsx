import type { Member } from '../../types/member'

interface ChoresTabProps {
  currentMember: Member | null
  userName?: string
}

export function ChoresTab({ currentMember, userName }: ChoresTabProps) {
  return (
    <div className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <h2 className="text-lg font-semibold mb-2">Zadania domowe</h2>
      <p className="text-sm text-slate-600 dark:text-slate-400">
        {`Cześć, ${currentMember?.name || userName || 'domowniku'}! Tutaj pojawią się Twoje zadania ze wskaźnikami świeżości.`}
      </p>
    </div>
  )
}
