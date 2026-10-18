import { Component, type ErrorInfo, type ReactNode } from 'react'

interface ErrorBoundaryProps {
  children: ReactNode
}

interface ErrorBoundaryState {
  hasError: boolean
}

// Catches render errors anywhere in the app so a single bad response shows a recoverable screen instead of a blank page.
export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  state: ErrorBoundaryState = { hasError: false }

  static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Unhandled render error:', error, info.componentStack)
  }

  render() {
    if (!this.state.hasError) {
      return this.props.children
    }

    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 text-slate-800 dark:text-slate-200 p-4">
        <div className="max-w-md w-full bg-white dark:bg-slate-900 border border-red-200 dark:border-red-900 rounded-2xl p-6 shadow-sm">
          <h2 className="text-lg font-bold text-red-600 dark:text-red-400 mb-2">Coś poszło nie tak</h2>
          <p className="text-sm text-slate-600 dark:text-slate-400 mb-4">
            Aplikacja napotkała nieoczekiwany błąd. Odśwież stronę, aby spróbować ponownie.
          </p>
          <button
            onClick={() => window.location.reload()}
            className="w-full py-2 px-4 bg-amber-500 hover:bg-amber-600 text-white font-medium rounded-xl transition cursor-pointer"
          >
            Odśwież stronę
          </button>
        </div>
      </div>
    )
  }
}
