import type { MemberScoreboardItem } from '../../types/scoreboard'

interface WorkShareBarProps {
  members: MemberScoreboardItem[]
}

const SEGMENT_COLORS = [
  {
    bg: 'bg-emerald-500 dark:bg-emerald-400',
    indicator: 'bg-emerald-500',
    text: 'text-emerald-700 dark:text-emerald-400',
  },
  {
    bg: 'bg-amber-500 dark:bg-amber-400',
    indicator: 'bg-amber-500',
    text: 'text-amber-700 dark:text-amber-400',
  },
  {
    bg: 'bg-sky-500 dark:bg-sky-400',
    indicator: 'bg-sky-500',
    text: 'text-sky-700 dark:text-sky-400',
  },
  {
    bg: 'bg-purple-500 dark:bg-purple-400',
    indicator: 'bg-purple-500',
    text: 'text-purple-700 dark:text-purple-400',
  },
]

export function WorkShareBar({ members }: WorkShareBarProps) {
  const totalPoints = members.reduce((sum, m) => sum + m.monthlyPoints, 0)

  return (
    <div
      data-testid="work-share-card"
      className="bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-4 shadow-xs flex flex-col gap-3"
    >
      <div className="flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-stone-500 dark:text-stone-400">
          Udział w obowiązkach
        </h3>
        <span className="text-xs font-medium text-stone-500 dark:text-stone-400">
          Łącznie: {totalPoints} pkt
        </span>
      </div>

      {/* Segmented bar */}
      <div className="h-4 w-full bg-stone-100 dark:bg-stone-800 rounded-full overflow-hidden flex">
        {totalPoints === 0 ? (
          <div className="w-full h-full bg-stone-200 dark:bg-stone-800" />
        ) : (
          members.map((member, index) => {
            const color = SEGMENT_COLORS[index % SEGMENT_COLORS.length]
            const width = `${member.workSharePercentage}%`
            if (member.workSharePercentage <= 0) return null

            return (
              <div
                key={member.memberId}
                style={{ width }}
                title={`${member.name}: ${member.workSharePercentage}%`}
                className={`h-full ${color.bg} transition-all duration-300 first:rounded-l-full last:rounded-r-full`}
              />
            )
          })
        )}
      </div>

      {/* Legend */}
      <div className="flex items-center justify-between gap-2 flex-wrap pt-1">
        {members.map((member, index) => {
          const color = SEGMENT_COLORS[index % SEGMENT_COLORS.length]
          return (
            <div
              key={member.memberId}
              data-testid={`work-share-member-${member.memberId}`}
              className="flex items-center gap-2 text-xs"
            >
              <span className={`w-2.5 h-2.5 rounded-full ${color.indicator} shrink-0`} />
              <span className="font-medium text-stone-700 dark:text-stone-300">{member.name}</span>
              <span className="font-bold text-stone-900 dark:text-stone-100">
                {member.workSharePercentage}%
              </span>
              <span className="text-[11px] text-stone-400 dark:text-stone-500">
                ({member.monthlyPoints} pkt)
              </span>
            </div>
          )
        })}
      </div>
    </div>
  )
}
