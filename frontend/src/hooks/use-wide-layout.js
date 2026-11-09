import { useEffect, useState } from 'react'

export function useWideLayout() {
  const [wide, setWide] = useState(
    () => window.matchMedia?.('(min-width: 1280px)').matches ?? false,
  )
  useEffect(() => {
    const media = window.matchMedia?.('(min-width: 1280px)')
    if (!media) return
    const change = () => setWide(media.matches)
    media.addEventListener('change', change)
    return () => media.removeEventListener('change', change)
  }, [])
  return wide
}
