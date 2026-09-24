import { Sparkles } from 'lucide-react'

interface LoginScreenProps {
  onLogin: () => Promise<void> | void
}

export function LoginScreen({ onLogin }: LoginScreenProps) {
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
          onClick={() => onLogin()}
          className="w-full py-3 px-4 bg-amber-500 hover:bg-amber-600 text-white font-semibold rounded-2xl transition shadow-sm hover:shadow cursor-pointer"
        >
          Zaloguj się
        </button>
      </div>
    </div>
  )
}
