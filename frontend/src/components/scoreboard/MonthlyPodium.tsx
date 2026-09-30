import { Trophy } from 'lucide-react'
import type { MemberScoreboardItem } from '../../types/scoreboard'

interface MonthlyPodiumProps {
  members: MemberScoreboardItem[]
}

function getRankBadge(rank: number) {
  if (rank === 1) {
    return (
      <div className="w-8 h-8 rounded-full bg-amber-100 dark:bg-amber-900/40 text-amber-600 dark:text-amber-400 flex items-center justify-center text-base font-bold shadow-xs">
        🥇
      </div>
    )
  }
  if (rank === 2) {
    return (
      <div className="w-8 h-8 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-300 flex items-center justify-center text-base font-bold shadow-xs">
        🥈
      </div>
    )
  }
  if (rank === 3) {
    return (
      <div className="w-8 h-8 rounded-full bg-amber-800/10 dark:bg-amber-900/20 text-amber-800 dark:text-amber-600 flex items-center justify-center text-base font-bold shadow-xs">
        🥉
      </div>
    )
  }
  return (
    <div className="w-8 h-8 rounded-full bg-stone-100 dark:bg-stone-800 text-stone-500 dark:text-stone-400 flex items-center justify-center text-xs font-bold">
      #{rank}
    </div>
  )
}

export function MonthlyPodium({ members }: MonthlyPodiumProps) {
  return (
    <div
      data-testid="monthly-podium"
      className="bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-4 shadow-xs flex flex-col gap-3"
    >
      <div className="flex items-center gap-2 text-stone-900 dark:text-stone-100">
        <Trophy className="h-4 w-4 text-amber-500" />
        <h3 className="text-xs font-semibold uppercase tracking-wider text-stone-500 dark:text-stone-400">
          Miesięczne podium
        </h3>
      </div>

      <div className="flex flex-col gap-2">
        {members.map((member) => (
          <div
            key={member.memberId}
            data-testid={`podium-row-${member.memberId}`}
            className={`flex items-center justify-between p-3 rounded-xl border transition-colors ${
              member.rank === 1
                ? 'bg-amber-50/50 dark:bg-amber-950/20 border-amber-200/70 dark:border-amber-800/40'
                : 'bg-stone-50/60 dark:bg-stone-900/40 border-stone-200/60 dark:border-stone-800/60'
            }`}
          >
            <div className="flex items-center gap-3 min-w-0">
              {getRankBadge(member.rank)}
              <div className="flex flex-col min-w-0">
                <span className="text-sm font-bold text-stone-900 dark:text-stone-100 truncate">
                  {member.name}
                </span>
                <span className="text-[11px] text-stone-500 dark:text-stone-400">
                  {member.monthlyChoresCompleted}{' '}
                  {member.monthlyChoresCompleted === 1 ? 'zadanie' : 'zadań'}
                </span>
              </div>
            </div>

            <div className="flex items-center gap-1.5 shrink-0 pl-2">
              <span className="text-base font-extrabold text-stone-900 dark:text-stone-100">
                {member.monthlyPoints}
              </span>
              <span className="text-xs font-medium text-stone-500 dark:text-stone-400">pkt</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
