import * as Tabs from '@radix-ui/react-tabs'
import { BottomNav } from '../layout/BottomNav'
import { ChoresTab } from './ChoresTab'
import { ScoreboardTab } from './ScoreboardTab'
import { StoreTab } from './StoreTab'
import { useTabNavigation } from '../../hooks/useTabNavigation'

export function MainTabs() {
  const { activeTab, handleTabChange } = useTabNavigation()

  return (
    <Tabs.Root
      value={activeTab}
      onValueChange={handleTabChange}
      className="flex flex-col flex-1"
    >
      <div className="flex-1 flex flex-col gap-4">
        <Tabs.Content value="chores" className="focus:outline-none">
          <ChoresTab />
        </Tabs.Content>

        <Tabs.Content value="scoreboard" className="focus:outline-none">
          <ScoreboardTab />
        </Tabs.Content>

        <Tabs.Content value="store" className="focus:outline-none">
          <StoreTab />
        </Tabs.Content>
      </div>

      <BottomNav />
    </Tabs.Root>
  )
}

