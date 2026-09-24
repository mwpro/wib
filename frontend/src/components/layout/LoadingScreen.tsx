interface LoadingScreenProps {
  message?: string
}

export function LoadingScreen({ message = 'Uwierzytelnianie...' }: LoadingScreenProps) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 text-slate-600 dark:text-slate-400">
      <div className="flex flex-col items-center gap-3">
        <div className="w-8 h-8 border-4 border-amber-500 border-t-transparent rounded-full animate-spin" />
        <p className="text-sm font-medium">{message}</p>
      </div>
    </div>
  )
}
