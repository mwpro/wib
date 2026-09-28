import { useState, useRef, useEffect } from 'react'
import { Sparkles, LogOut, Coins, User, FlaskConical } from 'lucide-react'
import { useAuth } from '../../auth/useAuth'
import type { Member } from '../../types/member'

interface HeaderProps {
  currentMember: Member | null
}

export function Header({ currentMember }: HeaderProps) {
  const { user, isTestMode, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false)
      }
    }
    if (menuOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [menuOpen])

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

        {/* Profile Avatar with Dropdown Menu */}
        <div className="relative pl-2 border-l border-slate-200 dark:border-slate-800" ref={menuRef}>
          <button
            onClick={() => setMenuOpen(!menuOpen)}
            title="Profil i menu użytkownika"
            className="w-8 h-8 rounded-full bg-amber-100 dark:bg-amber-950 text-amber-800 dark:text-amber-300 border border-amber-200 dark:border-amber-800 flex items-center justify-center font-bold text-xs hover:ring-2 hover:ring-amber-500/30 transition cursor-pointer"
          >
            {initials || <User className="h-4 w-4" />}
          </button>

          {menuOpen && (
            <div className="absolute right-0 top-10 z-30 w-48 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-xl py-2 px-1 text-sm animate-in fade-in duration-100">
              <div className="px-3 py-1.5 border-b border-slate-100 dark:border-slate-800 mb-1">
                <p className="font-semibold text-xs text-slate-900 dark:text-slate-100 truncate">
                  {user?.name || 'Użytkownik'}
                </p>
              </div>
              <button
                onClick={() => {
                  setMenuOpen(false)
                  logout()
                }}
                className="w-full px-3 py-2 text-left flex items-center gap-2 text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition cursor-pointer text-xs font-medium"
              >
                <LogOut className="h-3.5 w-3.5 text-slate-500" />
                <span>Wyloguj się</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
