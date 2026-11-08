import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { CheckCircle2, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '@/stores/auth-store'
import { dismissToast, useUiFeedbackStore } from '@/stores/ui-feedback-store'
import { Button } from './ui/button'

export function ToastViewport() {
  const { t } = useTranslation()
  const userId = useAuthStore((state) => state.access?.userId)
  const notice = useUiFeedbackStore((state) => state.notice)
  const [paused, setPaused] = useState(false)
  const ref = useRef(null)
  const [top, setTop] = useState(16)
  useLayoutEffect(() => {
    if (!notice) return
    const position = () => {
      const obstacles = [...document.querySelectorAll('.thread-header, .page-header, .settings-tabs')].map((node) => node.getBoundingClientRect()).filter((rect) => rect.width && rect.bottom > 0 && rect.top < innerHeight)
      const preferred = Math.max(16, ...obstacles.map((rect) => rect.bottom + 12))
      const composer = document.querySelector('.composer')?.getBoundingClientRect()
      const limit = composer ? composer.top - (ref.current?.offsetHeight || 80) - 12 : innerHeight - 160
      setTop(Math.max(16, Math.min(preferred, limit)))
    }
    position()
    window.addEventListener('resize', position)
    window.visualViewport?.addEventListener('resize', position)
    return () => { window.removeEventListener('resize', position); window.visualViewport?.removeEventListener('resize', position) }
  }, [notice])
  useEffect(() => {
    if (!notice) return
    if (notice.userId !== userId) { dismissToast(notice.id); return }
    if (!paused) { const timer = setTimeout(() => dismissToast(notice.id), 2000); return () => clearTimeout(timer) }
  }, [notice, userId, paused])
  if (!notice || notice.userId !== userId) return null
  return <div ref={ref} className="toast-viewport" style={{ top }}><div className="app-toast" onPointerEnter={() => setPaused(true)} onPointerLeave={() => setPaused(false)} onFocus={() => setPaused(true)} onBlur={(event) => { if (!event.currentTarget.contains(event.relatedTarget)) setPaused(false) }}>
    <CheckCircle2 size={20} aria-hidden="true" /><p role="status">{notice.message}</p><Button variant="ghost" size="icon" aria-label={t('common:closeToast')} onClick={() => { dismissToast(notice.id); setPaused(false) }}><X size={17} /></Button>
  </div></div>
}
