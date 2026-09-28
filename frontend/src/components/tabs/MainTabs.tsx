import * as Tabs from '@radix-ui/react-tabs'
import { BottomNav } from '../layout/BottomNav'
import { ChoresTab } from './ChoresTab'
import { ScoreboardTab } from './ScoreboardTab'
import { StoreTab } from './StoreTab'
export function MainTabs() {
  return (
    <Tabs.Root defaultValue="chores" className="flex flex-col flex-1">
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

