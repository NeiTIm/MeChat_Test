import { useCallback, useLayoutEffect, useRef, useState } from 'react'

export function useThreadScroll(messages, conversationId, userId, anchored = false) {
  const scrollRef = useRef(null)
  const previous = useRef({})
  const atBottom = useRef(true)
  const [newMessages, setNewMessages] = useState(false)
  const hasMessages = messages.length > 0
  const jumpToLatest = useCallback(() => {
    const element = scrollRef.current
    if (element) element.scrollTop = element.scrollHeight
    atBottom.current = true
    setNewMessages(false)
  }, [])
  const onScroll = useCallback(() => {
    const element = scrollRef.current
    if (!element) return
    atBottom.current = element.scrollHeight - element.clientHeight - element.scrollTop < 72
    if (atBottom.current) setNewMessages(false)
  }, [])

  useLayoutEffect(() => {
    const element = scrollRef.current
    if (!element) return
    const first = messages[0]?.id
    const last = messages.at(-1)
    const old = previous.current
    if (anchored) {
      // Explicit message targets own the scroll position, including pending loads.
      if (old.id === conversationId && old.last === last?.id && old.first !== first) element.scrollTop += element.scrollHeight - old.height
      else if (old.id === conversationId && old.last && old.last !== last?.id) setNewMessages(true)
    } else if (old.id !== conversationId || !old.last || old.anchored) jumpToLatest()
    else if (old.first !== first && old.last === last?.id) {
      // Preserve the viewport anchor when older history is prepended.
      element.scrollTop += element.scrollHeight - old.height
    } else if (old.last !== last?.id) {
      if (atBottom.current || last?.senderUserId === userId) jumpToLatest()
      else setNewMessages(true)
    } else if (atBottom.current) element.scrollTop = element.scrollHeight
    previous.current = { id: conversationId, first, last: last?.id, height: element.scrollHeight, anchored }
  }, [messages, conversationId, userId, jumpToLatest, anchored])

  useLayoutEffect(() => {
    const element = scrollRef.current
    if (!element || anchored || typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(() => { if (atBottom.current) element.scrollTop = element.scrollHeight })
    observer.observe(element)
    return () => observer.disconnect()
  }, [conversationId, hasMessages, anchored])
  return { scrollRef, onScroll, newMessages, jumpToLatest }
}
