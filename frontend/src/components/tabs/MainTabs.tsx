import * as Tabs from '@radix-ui/react-tabs'
import { BottomNav } from '../layout/BottomNav'
import { ChoresTab } from './ChoresTab'
import { ScoreboardTab } from './ScoreboardTab'
import { StoreTab } from './StoreTab'
import type { Member } from '../../types/member'

interface MainTabsProps {
  currentMember: Member | null
  userName?: string
}

export function MainTabs({ currentMember, userName }: MainTabsProps) {
  return (
    <Tabs.Root defaultValue="chores" className="flex flex-col gap-4">
      <Tabs.Content value="chores" className="focus:outline-none">
        <ChoresTab currentMember={currentMember} userName={userName} />
      </Tabs.Content>

      <Tabs.Content value="scoreboard" className="focus:outline-none">
        <ScoreboardTab />
      </Tabs.Content>

      <Tabs.Content value="store" className="focus:outline-none">
        <StoreTab />
      </Tabs.Content>

      <BottomNav />
    </Tabs.Root>
  )
}
