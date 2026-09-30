import { Flame, CheckCircle2 } from 'lucide-react'
import type { MemberScoreboardItem } from '../../types/scoreboard'

interface LifetimeStatsProps {
  members: MemberScoreboardItem[]
}

export function LifetimeStats({ members }: LifetimeStatsProps) {
  return (
    <div
      data-testid="lifetime-stats"
      className="bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-4 shadow-xs flex flex-col gap-3"
    >
      <div className="flex items-center gap-2">
        <Flame className="h-4 w-4 text-orange-500" />
        <h3 className="text-xs font-semibold uppercase tracking-wider text-stone-500 dark:text-stone-400">
          Statystyki wszech czasów
        </h3>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {members.map((member) => (
          <div
            key={member.memberId}
            data-testid={`lifetime-card-${member.memberId}`}
            className="p-3.5 rounded-xl bg-stone-50/80 dark:bg-stone-900/50 border border-stone-200/60 dark:border-stone-800/60 flex flex-col gap-2.5"
          >
            <span className="text-xs font-bold text-stone-800 dark:text-stone-200 truncate">
              {member.name}
            </span>

            <div className="grid grid-cols-2 gap-2 text-center pt-0.5">
              <div className="flex flex-col items-center bg-white dark:bg-stone-800/70 p-2 rounded-lg border border-stone-200/50 dark:border-stone-700/50">
                <span className="text-sm font-extrabold text-stone-900 dark:text-stone-100">
                  {member.lifetimePoints}
                </span>
                <span className="text-[10px] text-stone-500 dark:text-stone-400 font-medium">
                  Łącznie pkt
                </span>
              </div>

              <div className="flex flex-col items-center bg-white dark:bg-stone-800/70 p-2 rounded-lg border border-stone-200/50 dark:border-stone-700/50">
                <span className="text-sm font-extrabold text-stone-900 dark:text-stone-100 flex items-center gap-1">
                  <CheckCircle2 className="h-3 w-3 text-emerald-500" />
                  {member.lifetimeChoresCompleted}
                </span>
                <span className="text-[10px] text-stone-500 dark:text-stone-400 font-medium">
                  Zadań
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
