import { useEffect, useState } from 'react'
import { useAuth } from '../auth/useAuth'
import { getCurrentMember } from '../api/client'
import type { Member } from '../types/member'

export function useCurrentMember() {
  const auth = useAuth()
  const { isAuthenticated, user } = auth
  const [member, setMember] = useState<Member | null>(null)
  const [loadingMember, setLoadingMember] = useState(false)

  useEffect(() => {
    if (!isAuthenticated) return

    let isMounted = true
    getCurrentMember(auth)
      .then((data) => {
        if (isMounted) {
          setMember(data)
          setLoadingMember(false)
        }
      })
      .catch((err) => {
        if (isMounted) {
          console.error('Failed to load current member:', err)
          setLoadingMember(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [isAuthenticated, user?.sub, auth])

  return {
    currentMember: isAuthenticated ? member : null,
    loadingMember,
  }
}
