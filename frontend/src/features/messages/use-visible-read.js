import { useEffect, useRef } from 'react'

// Acknowledge only bubbles actually visible in a focused foreground thread.
export function useVisibleRead({ scrollRef, messages, selectedId, onRead }) {
  const callback = useRef(onRead)
  callback.current = onRead
  useEffect(() => {
    const root = scrollRef.current
    if (!root || !onRead) return
    const visible = new Map()
    let timer
    const notify = () => {
      clearTimeout(timer)
      timer = setTimeout(() => {
        if (document.visibilityState !== 'visible' || !document.hasFocus() || !root.getClientRects().length) return
        const sequence = Math.max(0, ...visible.values())
        if (sequence) callback.current?.(sequence)
      }, 350)
    }
    const observer = new IntersectionObserver((entries) => {
      for (const entry of entries) {
        const message = messages.find((item) => item.id === entry.target.dataset.messageId)
        if (!message) continue
        if (entry.isIntersecting && entry.intersectionRatio >= .6) visible.set(message.id, message.sequence)
        else visible.delete(message.id)
      }
      notify()
    }, { root, threshold: [.6] })
    root.querySelectorAll('[data-message-id]').forEach((element) => observer.observe(element))
    window.addEventListener('focus', notify)
    document.addEventListener('visibilitychange', notify)
    return () => { observer.disconnect(); clearTimeout(timer); window.removeEventListener('focus', notify); document.removeEventListener('visibilitychange', notify) }
  }, [scrollRef, messages, selectedId, onRead])
}
