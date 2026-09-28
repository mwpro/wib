import { useState, useRef, useEffect } from 'react'
import { Broom, LogOut, Coins, User, FlaskConical } from 'lucide-react'
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
    <header className="border-b border-stone-200/80 dark:border-stone-800/80 bg-white/90 dark:bg-[#0c0e12]/90 backdrop-blur-md sticky top-0 z-20 px-4 py-3 flex items-center justify-between transition-colors">
      <div className="flex items-center gap-2.5">
        {/* Chore brand mark (Broom icon in warm amber badge) */}
        <div className="w-8 h-8 rounded-xl bg-amber-100 dark:bg-amber-950/80 text-amber-900 dark:text-amber-300 border border-amber-300 dark:border-amber-800 flex items-center justify-center shadow-xs select-none">
          <Broom className="w-4.5 h-4.5" />
        </div>
        <div className="flex flex-col -space-y-0.5">
          <div className="flex items-center gap-2">
            <h1 className="text-lg font-black tracking-tight text-stone-900 dark:text-stone-100">wib</h1>
            {isTestMode && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-100 text-amber-950 dark:bg-amber-950 dark:text-amber-300 border border-amber-300 dark:border-amber-800">
                <FlaskConical className="h-3 w-3 text-amber-700 dark:text-amber-400" />
                Test Mode
              </span>
            )}
          </div>
          <span className="text-[11px] font-medium text-stone-500 dark:text-stone-400 hidden sm:inline">
            Who is Better
          </span>
        </div>
      </div>

      <div className="flex items-center gap-3">
        {currentMember && (
          <div className="flex items-center">
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-amber-100/80 dark:bg-amber-950/60 text-amber-950 dark:text-amber-300 border border-amber-300/80 dark:border-amber-800/80 text-xs font-bold tracking-tight shadow-xs">
              <Coins className="h-3.5 w-3.5 text-amber-700 dark:text-amber-400" />
              <span>{currentMember.walletBalance} pkt</span>
            </span>
          </div>
        )}

        {/* Profile Avatar with Dropdown Menu */}
        <div className="relative pl-2.5 border-l border-stone-200 dark:border-stone-800" ref={menuRef}>
          <button
            type="button"
            onClick={() => setMenuOpen(!menuOpen)}
            title="Profil i menu użytkownika"
            className="w-8 h-8 rounded-full bg-amber-100 dark:bg-amber-950 text-amber-900 dark:text-amber-300 border border-amber-300 dark:border-amber-800 flex items-center justify-center font-bold text-xs hover:border-amber-500 hover:ring-2 hover:ring-amber-500/20 transition cursor-pointer"
          >
            {initials || <User className="h-4 w-4" />}
          </button>

          {menuOpen && (
            <div className="absolute right-0 top-10 z-30 w-48 bg-white dark:bg-stone-900 border border-stone-200 dark:border-stone-800 rounded-2xl shadow-xl py-1.5 px-1 text-sm animate-in fade-in duration-100">
              <div className="px-3 py-1.5 border-b border-stone-100 dark:border-stone-800 mb-1">
                <p className="font-semibold text-xs text-stone-900 dark:text-stone-100 truncate">
                  {user?.name || 'Użytkownik'}
                </p>
              </div>
              <button
                type="button"
                onClick={() => {
                  setMenuOpen(false)
                  logout()
                }}
                className="w-full px-3 py-2 text-left flex items-center gap-2 text-stone-700 dark:text-stone-300 hover:bg-stone-100 dark:hover:bg-stone-800 rounded-xl transition cursor-pointer text-xs font-medium"
              >
                <LogOut className="h-3.5 w-3.5 text-stone-400" />
                <span>Wyloguj się</span>
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
