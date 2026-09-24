import * as Tabs from '@radix-ui/react-tabs'
import { CheckCircle, Trophy, ShoppingBag } from 'lucide-react'
import { ChoresTab } from './ChoresTab'
import { ScoreboardTab } from './ScoreboardTab'
import { StoreTab } from './StoreTab'
import type { Member } from '../../types/member'

interface MainTabsProps {
  currentMember: Member | null
  loadingMember: boolean
  userName?: string
}

export function MainTabs({ currentMember, loadingMember, userName }: MainTabsProps) {
  return (
    <Tabs.Root defaultValue="chores" className="flex flex-col gap-4">
      <Tabs.List className="grid grid-cols-3 gap-1 bg-slate-200 dark:bg-slate-800 p-1 rounded-xl">
        <Tabs.Trigger
          value="chores"
          className="flex items-center justify-center gap-2 py-2 text-sm font-semibold rounded-lg data-[state=active]:bg-white dark:data-[state=active]:bg-slate-950 data-[state=active]:shadow-sm transition cursor-pointer"
        >
          <CheckCircle className="h-4 w-4" />
          <span>Zadania</span>
        </Tabs.Trigger>
        <Tabs.Trigger
          value="scoreboard"
          className="flex items-center justify-center gap-2 py-2 text-sm font-semibold rounded-lg data-[state=active]:bg-white dark:data-[state=active]:bg-slate-950 data-[state=active]:shadow-sm transition cursor-pointer"
        >
          <Trophy className="h-4 w-4" />
          <span>Kto jest lepszy?</span>
        </Tabs.Trigger>
        <Tabs.Trigger
          value="store"
          className="flex items-center justify-center gap-2 py-2 text-sm font-semibold rounded-lg data-[state=active]:bg-white dark:data-[state=active]:bg-slate-950 data-[state=active]:shadow-sm transition cursor-pointer"
        >
          <ShoppingBag className="h-4 w-4" />
          <span>Sklep</span>
        </Tabs.Trigger>
      </Tabs.List>

      <Tabs.Content value="chores">
        <ChoresTab currentMember={currentMember} loadingMember={loadingMember} userName={userName} />
      </Tabs.Content>

      <Tabs.Content value="scoreboard">
        <ScoreboardTab />
      </Tabs.Content>

      <Tabs.Content value="store">
        <StoreTab />
      </Tabs.Content>
    </Tabs.Root>
  )
}
