import { useAuth } from './auth/useAuth'
import { useCurrentMember } from './hooks/useCurrentMember'
import { Header } from './components/layout/Header'
import { LoadingScreen } from './components/layout/LoadingScreen'
import { LoginScreen } from './components/auth/LoginScreen'
import { MainTabs } from './components/tabs/MainTabs'

export function App() {
  const { isAuthenticated, isLoading, user, login } = useAuth()
  const { currentMember } = useCurrentMember()

  if (isLoading) {
    return <LoadingScreen />
  }

  if (!isAuthenticated) {
    return <LoginScreen onLogin={login} />
  }

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100 flex flex-col">
      <Header currentMember={currentMember} />
      <main className="flex-1 max-w-2xl w-full mx-auto p-4 flex flex-col gap-6">
        <MainTabs
          currentMember={currentMember}
          userName={user?.name}
        />
      </main>
    </div>
  )
}

export default App
