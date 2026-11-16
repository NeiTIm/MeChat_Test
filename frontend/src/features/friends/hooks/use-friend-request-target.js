import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { useAuthStore } from '@/stores/auth-store'

export function useFriendRequestTarget({
  requestId,
  requestKey,
  requests,
  userId,
  enabled,
  containerRef,
}) {
  const completed = useRef(null)
  const [highlight, setHighlight] = useState(null)
  const key = `${userId}:${requestId}:${requestKey}`
  const target = requests.data?.incoming?.find(
    (item) => item.id === requestId && item.receiverUserId === userId && item.status === 'Pending',
  )
  useLayoutEffect(() => {
    if (
      !enabled ||
      !target ||
      requests.error ||
      requests.isFetching ||
      completed.current === key ||
      useAuthStore.getState().access?.userId !== userId
    )
      return
    const node = [
      ...(containerRef.current?.querySelectorAll('[data-friend-request-id]') || []),
    ].find((item) => item.dataset.friendRequestId === requestId)
    if (!node) return
    node.scrollIntoView({ block: 'center', behavior: 'instant' })
    node.focus({ preventScroll: true })
    completed.current = key
    setHighlight({ key, id: requestId })
  }, [enabled, target, requests.error, requests.isFetching, key, userId, requestId, containerRef])
  useEffect(() => {
    if (!highlight || highlight.key !== key) return
    const timer = setTimeout(() => setHighlight(null), 2500)
    return () => clearTimeout(timer)
  }, [highlight, key])
  return {
    highlightedId: enabled && highlight?.key === key ? highlight.id : null,
    unavailable:
      enabled &&
      !!requestId &&
      !target &&
      !requests.isPending &&
      !requests.isFetching &&
      !requests.error,
  }
}
