import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuthStore } from '@/stores/auth-store'
import { queryClient } from '@/lib/query-client'
import { bcrFeatures } from '@/lib/bcr-features'
import { friendsApi } from './api'
import { friendUnreadKey } from './use-friend-unread'
import { createSeenQueue } from './seen-queue'

export function useRequestSeen(enabled) {
  const userId = useAuthStore((state) => state.access?.userId)
  const nodes = useRef(new Map())
  const queue = useRef(null)
  const [failure, setFailure] = useState(null)
  useEffect(() => {
    if (!enabled || !userId || !bcrFeatures.friendSeen) return
    const current = createSeenQueue({
      send: friendsApi.markSeen,
      isCurrent: () => useAuthStore.getState().access?.userId === userId,
      confirmed: () => {
        setFailure(null)
        queryClient.invalidateQueries({ queryKey: friendUnreadKey(userId) })
        queryClient.invalidateQueries({ queryKey: ['workspace', 'friendRequests', userId] })
      },
      failed: (error) => setFailure({ userId, error }),
    })
    queue.current = current
    nodes.current.forEach((node, id) => current.register(id, node))
    const unsubscribe = useAuthStore.subscribe((state, old) => {
      if (state.access?.userId !== old.access?.userId) current.stop()
    })
    return () => {
      unsubscribe()
      current.stop()
      if (queue.current === current) queue.current = null
    }
  }, [enabled, userId])
  const register = useCallback((id, node) => {
    if (node) nodes.current.set(id, node)
    else nodes.current.delete(id)
    queue.current?.register(id, node)
  }, [])
  return {
    register,
    error: enabled && failure && failure.userId === userId ? failure.error : null,
    retry: () => queue.current?.retry(),
  }
}
