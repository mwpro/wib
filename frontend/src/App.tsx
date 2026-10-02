import { useEffect } from 'react'
import { useAuth } from './auth/useAuth'
import { useCurrentMember } from './hooks/useCurrentMember'
import { Header } from './components/layout/Header'
import { LoadingScreen } from './components/layout/LoadingScreen'
import { MainTabs } from './components/tabs/MainTabs'

export function App() {
  const { isAuthenticated, isLoading, login } = useAuth()
  const { currentMember } = useCurrentMember()

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      login()
    }
  }, [isLoading, isAuthenticated, login])

  if (isLoading || !isAuthenticated) {
    return <LoadingScreen />
  }

  return (
    <div className="min-h-screen bg-[#f8f9fa] text-stone-900 dark:bg-[#0c0e12] dark:text-stone-100 flex flex-col transition-colors selection:bg-amber-500/20 selection:text-amber-900 dark:selection:text-amber-200">
      <Header currentMember={currentMember} />
      <main className="flex-1 max-w-2xl w-full mx-auto p-4 pb-24 flex flex-col gap-6">
        <MainTabs />
      </main>
    </div>
  )
}

export default App
