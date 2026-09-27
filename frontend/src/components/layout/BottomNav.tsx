import * as Tabs from '@radix-ui/react-tabs'
import { CheckCircle, Trophy, ShoppingBag } from 'lucide-react'

export function BottomNav() {
  return (
    <nav className="fixed bottom-0 left-0 right-0 z-30 bg-white/95 dark:bg-slate-900/95 backdrop-blur border-t border-slate-200 dark:border-slate-800 pb-[max(0.75rem,env(safe-area-inset-bottom))] pt-2 px-4 shadow-lg">
      <div className="max-w-2xl mx-auto">
        <Tabs.List className="grid grid-cols-3 gap-2">
          <Tabs.Trigger
            value="chores"
            className="flex flex-col items-center justify-center gap-1 py-1.5 px-2 text-xs font-semibold rounded-xl text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 data-[state=active]:bg-amber-50 dark:data-[state=active]:bg-amber-950/60 data-[state=active]:text-amber-800 dark:data-[state=active]:text-amber-300 transition cursor-pointer"
          >
            <CheckCircle className="h-5 w-5" />
            <span>Zadania</span>
          </Tabs.Trigger>
          <Tabs.Trigger
            value="scoreboard"
            className="flex flex-col items-center justify-center gap-1 py-1.5 px-2 text-xs font-semibold rounded-xl text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 data-[state=active]:bg-amber-50 dark:data-[state=active]:bg-amber-950/60 data-[state=active]:text-amber-800 dark:data-[state=active]:text-amber-300 transition cursor-pointer"
          >
            <Trophy className="h-5 w-5" />
            <span>Kto jest lepszy?</span>
          </Tabs.Trigger>
          <Tabs.Trigger
            value="store"
            className="flex flex-col items-center justify-center gap-1 py-1.5 px-2 text-xs font-semibold rounded-xl text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-100 data-[state=active]:bg-amber-50 dark:data-[state=active]:bg-amber-950/60 data-[state=active]:text-amber-800 dark:data-[state=active]:text-amber-300 transition cursor-pointer"
          >
            <ShoppingBag className="h-5 w-5" />
            <span>Sklep</span>
          </Tabs.Trigger>
        </Tabs.List>
      </div>
    </nav>
  )
}
