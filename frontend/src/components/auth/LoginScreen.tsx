import { Broom } from 'lucide-react'

interface LoginScreenProps {
  onLogin: () => void | Promise<void>
}

export function LoginScreen({ onLogin }: LoginScreenProps) {
  return (
    <div className="min-h-screen bg-stone-50 dark:bg-[#0c0e12] text-stone-900 dark:text-stone-100 flex flex-col items-center justify-center p-4">
      <div className="max-w-md w-full bg-white dark:bg-[#14161d] border border-stone-200/90 dark:border-stone-800/80 rounded-2xl p-8 shadow-sm flex flex-col items-center text-center gap-6">
        <div className="w-16 h-16 rounded-2xl bg-amber-100 dark:bg-amber-950/80 text-amber-900 dark:text-amber-300 border border-amber-300 dark:border-amber-800 flex items-center justify-center shadow-xs">
          <Broom className="w-8 h-8" />
        </div>
        <div>
          <h1 className="text-2xl font-black tracking-tight mb-2 text-stone-900 dark:text-white">wib</h1>
          <p className="text-sm text-stone-600 dark:text-stone-400">
            Who is Better – grywalizacja i sprawiedliwy podział obowiązków domowych.
          </p>
        </div>
        <button
          type="button"
          onClick={() => onLogin()}
          className="w-full py-3 px-4 bg-amber-400 hover:bg-amber-500 dark:bg-amber-400 dark:hover:bg-amber-300 text-amber-950 font-bold rounded-xl border border-amber-500/40 transition shadow-xs cursor-pointer active:scale-98"
        >
          Zaloguj się
        </button>
      </div>
    </div>
  )
}
