import { useCallback, useEffect, useRef } from 'react'

export function useConversationTyping({ id, status, invoke, requestMode }) {
  const typingLastSent = useRef(0)
  useEffect(() => {
    if (!id || status !== 'connected') return
    invoke('JoinConversation', id)
    return () => {
      invoke('StopTyping', id)
      invoke('LeaveConversation', id)
    }
  }, [id, status, invoke])
  const onTyping = useCallback(
    (started) => {
      if (!id || requestMode) return
      if (started && Date.now() - typingLastSent.current < 2500) return
      typingLastSent.current = started ? Date.now() : 0
      invoke(started ? 'StartTyping' : 'StopTyping', id)
    },
    [id, requestMode, invoke],
  )

  return onTyping
}
