import { useState, useEffect, useCallback } from 'react'

export type TabValue = 'chores' | 'scoreboard' | 'store'

const PATH_TO_TAB: Record<string, TabValue> = {
  '/': 'chores',
  '/chores': 'chores',
  '/scoreboard': 'scoreboard',
  '/store': 'store',
}

const TAB_TO_PATH: Record<TabValue, string> = {
  chores: '/',
  scoreboard: '/scoreboard',
  store: '/store',
}

function getTabFromPathname(pathname: string): TabValue {
  const normalized = pathname.replace(/\/+$/, '') || '/'
  return PATH_TO_TAB[normalized] || 'chores'
}

export function useTabNavigation() {
  const [activeTab, setActiveTab] = useState<TabValue>(() =>
    getTabFromPathname(window.location.pathname)
  )

  useEffect(() => {
    const handlePopState = () => {
      setActiveTab(getTabFromPathname(window.location.pathname))
    }

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const handleTabChange = useCallback((value: string) => {
    const tab = value as TabValue
    setActiveTab(tab)
    const targetPath = TAB_TO_PATH[tab] || '/'
    if (window.location.pathname !== targetPath) {
      window.history.pushState(null, '', targetPath)
    }
  }, [])

  return {
    activeTab,
    handleTabChange,
  }
}
