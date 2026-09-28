import * as Tabs from '@radix-ui/react-tabs'
import { CheckCircle, Trophy, ShoppingBag } from 'lucide-react'

export function BottomNav() {
  return (
    <nav className="fixed bottom-0 left-0 right-0 z-30 bg-white/95 dark:bg-stone-900/95 backdrop-blur border-t border-stone-200 dark:border-stone-800 pb-[max(0.6rem,env(safe-area-inset-bottom))] pt-1.5 px-4 shadow-sm">
      <div className="max-w-2xl mx-auto">
        <Tabs.List className="grid grid-cols-3 gap-1">
          <Tabs.Trigger
            value="chores"
            className="flex flex-col items-center justify-center gap-1 py-1.5 px-2 text-xs font-semibold rounded-xl text-stone-500 dark:text-stone-400 hover:text-stone-900 dark:hover:text-stone-100 data-[state=active]:bg-amber-100/80 dark:data-[state=active]:bg-amber-950/60 data-[state=active]:text-amber-900 dark:data-[state=active]:text-amber-300 transition cursor-pointer select-none"
          >
            <CheckCircle className="h-4.5 w-4.5" />
            <span>Zadania</span>
          </Tabs.Trigger>
          <Tabs.Trigger
            value="scoreboard"
            className="flex flex-col items-center justify-center gap-1 py-1.5 px-2 text-xs font-semibold rounded-xl text-stone-500 dark:text-stone-400 hover:text-stone-900 dark:hover:text-stone-100 data-[state=active]:bg-amber-100/80 dark:data-[state=active]:bg-amber-950/60 data-[state=active]:text-amber-900 dark:data-[state=active]:text-amber-300 transition cursor-pointer select-none"
          >
            <Trophy className="h-4.5 w-4.5" />
            <span>Kto jest lepszy?</span>
          </Tabs.Trigger>
          <Tabs.Trigger
            value="store"
            className="flex flex-col items-center justify-center gap-1 py-1.5 px-2 text-xs font-semibold rounded-xl text-stone-500 dark:text-stone-400 hover:text-stone-900 dark:hover:text-stone-100 data-[state=active]:bg-amber-100/80 dark:data-[state=active]:bg-amber-950/60 data-[state=active]:text-amber-900 dark:data-[state=active]:text-amber-300 transition cursor-pointer select-none"
          >
            <ShoppingBag className="h-4.5 w-4.5" />
            <span>Sklep</span>
          </Tabs.Trigger>
        </Tabs.List>
      </div>
    </nav>
  )
}
