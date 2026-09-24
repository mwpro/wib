import { Sparkles, LogOut, Coins, User, FlaskConical } from 'lucide-react'
import { useAuth } from '../../auth/useAuth'
import type { Member } from '../../types/member'

interface HeaderProps {
  currentMember: Member | null
}

export function Header({ currentMember }: HeaderProps) {
  const { user, isTestMode, logout } = useAuth()

  const initials = user?.name
    ? user.name
        .split(' ')
        .map((p) => p[0])
        .filter(Boolean)
        .slice(0, 2)
        .join('')
        .toUpperCase()
    : null

  return (
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
          </div>
        )}

        <div className="flex items-center gap-2 pl-2 border-l border-slate-200 dark:border-slate-800">
          <div className="w-8 h-8 rounded-full bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-800 flex items-center justify-center font-bold text-xs">
            {initials || <User className="h-4 w-4" />}
          </div>
          <span className="text-sm font-medium hidden md:inline">{user?.name}</span>
          <button
            onClick={() => logout()}
            title="Wyloguj"
            className="p-1.5 rounded-lg text-slate-500 hover:text-slate-900 dark:hover:text-slate-100 hover:bg-slate-100 dark:hover:bg-slate-800 transition cursor-pointer"
          >
            <LogOut className="h-4 w-4" />
          </button>
        </div>
      </div>
    </header>
  )
}
