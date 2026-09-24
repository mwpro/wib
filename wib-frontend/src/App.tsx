import { useEffect, useState } from 'react'
import * as Tabs from '@radix-ui/react-tabs'
import { Sparkles, CheckCircle, Trophy, ShoppingBag, LogOut, Coins, Flame, User, FlaskConical } from 'lucide-react'
import { useAuth } from './auth/useAuth'
import { getCurrentMember } from './api/client'
import type { Member } from './types/member'

export function App() {
  const auth = useAuth()
  const { isAuthenticated, isLoading, user, isTestMode, login, logout } = auth
  const [member, setMember] = useState<Member | null>(null)
  const [loadingMember, setLoadingMember] = useState(false)

  useEffect(() => {
    if (!isAuthenticated) return

    let isMounted = true
    getCurrentMember(auth)
      .then((data) => {
        if (isMounted) {
          setMember(data)
          setLoadingMember(false)
        }
      })
      .catch((err) => {
        if (isMounted) {
          console.error('Failed to load current member:', err)
          setLoadingMember(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [isAuthenticated, user?.sub, auth])

  const currentMember = isAuthenticated ? member : null

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400">
        <div className="flex flex-col items-center gap-3">
          <div className="w-8 h-8 border-4 border-amber-500 border-t-transparent rounded-full animate-spin" />
          <p className="text-sm font-medium">Uwierzytelnianie...</p>
        </div>
      </div>
    )
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-slate-50 dark:bg-slate-950 text-slate-900 dark:text-slate-100 flex flex-col items-center justify-center p-4">
        <div className="max-w-md w-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl p-8 shadow-sm flex flex-col items-center text-center gap-6">
          <div className="w-16 h-16 rounded-2xl bg-amber-500/10 text-amber-500 flex items-center justify-center">
            <Sparkles className="h-10 w-10" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight mb-2">wib</h1>
            <p className="text-sm text-slate-600 dark:text-slate-400">
              Who is Better – grywalizacja i sprawiedliwy podział obowiązków domowych.
            </p>
          </div>
          <button
            onClick={() => login()}
            className="w-full py-3 px-4 bg-amber-500 hover:bg-amber-600 text-white font-semibold rounded-2xl transition shadow-sm hover:shadow"
          >
            Zaloguj się
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100 flex flex-col">
      <header className="border-b border-slate-200 dark:border-slate-800 bg-white/80 dark:bg-slate-900/80 backdrop-blur sticky top-0 z-10 px-4 py-3 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Sparkles className="h-6 w-6 text-amber-500" />
          <h1 className="text-xl font-bold tracking-tight">wib</h1>
          <span className="text-xs text-slate-500 font-medium hidden sm:inline">Who is Better</span>
          {isTestMode && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300 border border-amber-200 dark:border-amber-800">
              <FlaskConical className="h-3 w-3" />
              Test Mode
            </span>
          )}
        </div>

        <div className="flex items-center gap-3">
          {currentMember && (
            <div className="flex items-center gap-2 text-xs font-semibold">
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-400 border border-amber-200 dark:border-amber-900">
                <Coins className="h-3.5 w-3.5" />
                {currentMember.walletBalance} pkt
              </span>
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-orange-50 dark:bg-orange-950/60 text-orange-700 dark:text-orange-400 border border-orange-200 dark:border-orange-900">
                <Flame className="h-3.5 w-3.5" />
                {currentMember.earnedPoints} XP
              </span>
            </div>
          )}

          <div className="flex items-center gap-2 pl-2 border-l border-slate-200 dark:border-slate-800">
            {user?.picture ? (
              <img
                src={user.picture}
                alt={user.name || 'Avatar'}
                className="w-8 h-8 rounded-full border border-slate-200 dark:border-slate-700 object-cover"
              />
            ) : (
              <div className="w-8 h-8 rounded-full bg-slate-200 dark:bg-slate-700 flex items-center justify-center text-slate-600 dark:text-slate-300">
                <User className="h-4 w-4" />
              </div>
            )}
            <span className="text-sm font-medium hidden md:inline">{user?.name}</span>
            <button
              onClick={() => logout()}
              title="Wyloguj"
              className="p-1.5 rounded-lg text-slate-500 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
            >
              <LogOut className="h-4 w-4" />
            </button>
          </div>
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
              {loadingMember
                ? 'Ładowanie profilu domownika...'
                : `Cześć, ${currentMember?.name || user?.name || 'domowniku'}! Tutaj pojawią się Twoje zadania ze wskaźnikami świeżości.`}
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
