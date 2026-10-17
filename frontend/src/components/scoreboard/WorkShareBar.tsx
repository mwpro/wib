import type { MemberScoreboardItem } from '../../types/scoreboard'
import { SegmentedBar } from '../common/SegmentedBar'
import { segmentColor } from '../common/segmentColors'

interface WorkShareBarProps {
  members: MemberScoreboardItem[]
}

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

      <SegmentedBar
        size="large"
        segments={members.map((member, index) => ({
          key: String(member.memberId),
          label: member.name,
          value: member.monthlyPoints,
          percentage: member.workSharePercentage,
          detail: `(${member.monthlyPoints} pkt)`,
          barClass: segmentColor(index).bg,
          dotClass: segmentColor(index).indicator,
          testId: `work-share-member-${member.memberId}`,
        }))}
      />
    </div>
  )
}
