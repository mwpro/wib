import { useEffect, useRef, useState } from 'react'
import { useAuth } from '../auth/useAuth'
import { getCurrentMember } from '../api/client'
import type { Member } from '../types/member'

export function useCurrentMember() {
  const auth = useAuth()
  const authRef = useRef(auth)
  authRef.current = auth
  const { isAuthenticated, user } = auth
  const [member, setMember] = useState<Member | null>(null)

  useEffect(() => {
    if (!isAuthenticated) return

    let isMounted = true
    getCurrentMember(authRef.current)
      .then((data) => {
        if (isMounted) {
          setMember(data)
        }
      })
      .catch((err) => {
        if (isMounted) {
          console.error('Failed to load current member:', err)
        }
      })

    return () => {
      isMounted = false
    }
  }, [isAuthenticated, user?.sub])

  return {
    currentMember: isAuthenticated ? member : null,
  }
}
