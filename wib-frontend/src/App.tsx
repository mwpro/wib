import * as Tabs from '@radix-ui/react-tabs'
import { Sparkles, CheckCircle, Trophy, ShoppingBag } from 'lucide-react'

export function App() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100 flex flex-col">
      <header className="border-b border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 backdrop-blur sticky top-0 z-10 px-4 py-3 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Sparkles className="h-6 w-6 text-amber-500" />
          <h1 className="text-xl font-bold tracking-tight">wib</h1>
          <span className="text-xs text-slate-500 font-medium">Who is Better</span>
        </div>
      </header>

      <main className="flex-1 max-w-2xl w-full mx-auto p-4 flex flex-col gap-6">
        <Tabs.Root defaultValue="chores" className="flex flex-col gap-4">
          <Tabs.List className="grid grid-cols-3 gap-1 bg-slate-200 dark:bg-slate-800 p-1 rounded-xl">
            <Tabs.Trigger
              value="chores"
              className="flex items-center justify-center gap-2 py-2 text-sm font-semibold rounded-lg data-[state=active]:bg-white dark:data-[state=active]:bg-slate-950 data-[state=active]:shadow-sm transition"
            >
              <CheckCircle className="h-4 w-4" />
              <span>Zadania</span>
            </Tabs.Trigger>
            <Tabs.Trigger
              value="scoreboard"
              className="flex items-center justify-center gap-2 py-2 text-sm font-semibold rounded-lg data-[state=active]:bg-white dark:data-[state=active]:bg-slate-950 data-[state=active]:shadow-sm transition"
            >
              <Trophy className="h-4 w-4" />
              <span>Kto jest lepszy?</span>
            </Tabs.Trigger>
            <Tabs.Trigger
              value="store"
              className="flex items-center justify-center gap-2 py-2 text-sm font-semibold rounded-lg data-[state=active]:bg-white dark:data-[state=active]:bg-slate-950 data-[state=active]:shadow-sm transition"
            >
              <ShoppingBag className="h-4 w-4" />
              <span>Sklep</span>
            </Tabs.Trigger>
          </Tabs.List>

          <Tabs.Content value="chores" className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
            <h2 className="text-lg font-semibold mb-2">Zadania domowe</h2>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Witaj w wib! Tutaj pojawią się Twoje zadania domowe z dynamiczną świeżością i punktami.
            </p>
          </Tabs.Content>

          <Tabs.Content value="scoreboard" className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
            <h2 className="text-lg font-semibold mb-2">Miesięczny wyścig</h2>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Rywalizacja w bieżącym miesiącu (strefa czasowa Europe/Warsaw).
            </p>
          </Tabs.Content>

          <Tabs.Content value="store" className="p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
            <h2 className="text-lg font-semibold mb-2">Sklep z nagrodami</h2>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Wymieniaj punkty na nagrody i realizuj kupony.
            </p>
          </Tabs.Content>
        </Tabs.Root>
      </main>
    </div>
  )
}

export default App
