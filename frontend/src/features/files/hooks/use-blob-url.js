import { useEffect, useState } from 'react'

export function useBlobUrl(blob) {
  const [current, setCurrent] = useState(null)
  useEffect(() => {
    if (!blob) return
    const next = URL.createObjectURL(blob)
    setCurrent({ blob, url: next })
    return () => URL.revokeObjectURL(next)
  }, [blob])
  return current && current.blob === blob ? current.url : null
}
