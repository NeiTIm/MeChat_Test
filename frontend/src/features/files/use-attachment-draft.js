import { useEffect, useRef, useState } from 'react'
import { translate } from '@/i18n'
import { filesApi } from './api'
import { validateUpload } from './validation'

export function useAttachmentDraft() {
  const [items, setItems] = useState([])
  const [validationError, setValidationError] = useState('')
  const controllers = useRef(new Map())
  useEffect(() => {
    const current = controllers.current
    return () => {
      for (const controller of current.values()) controller.abort()
      current.clear()
    }
  }, [])
  async function upload(item) {
    const controller = new AbortController()
    controllers.current.set(item.key, controller)
    const update = (fields) => {
      if (!controller.signal.aborted)
        setItems((list) =>
          list.map((current) => (current.key === item.key ? { ...current, ...fields } : current)),
        )
    }
    update({ state: 'uploading', error: null })
    try {
      const stored = await filesApi.upload(item.file, 'Message', controller.signal, (progress) =>
        update({ progress }),
      )
      update({ state: 'ready', stored })
    } catch (error) {
      update({ state: 'error', error })
    } finally {
      controllers.current.delete(item.key)
    }
  }
  function add(files) {
    setValidationError('')
    const selected = [...files]
    if (selected.length + items.length > 5) {
      setValidationError(translate('files:countLimit'))
      return
    }
    const invalid = selected.map((file) => validateUpload(file)).find(Boolean)
    if (invalid) {
      setValidationError(invalid)
      return
    }
    const next = selected.map((file) => ({
      key: crypto.randomUUID(),
      file,
      state: 'uploading',
      progress: 0,
    }))
    setItems((list) => [...list, ...next])
    next.forEach(upload)
  }
  function remove(key) {
    controllers.current.get(key)?.abort()
    controllers.current.delete(key)
    setItems((list) => list.filter((item) => item.key !== key))
  }
  return {
    items,
    validationError,
    add,
    remove,
    retry: upload,
    clear: () => setItems([]),
    uploading: items.some((item) => item.state === 'uploading'),
    blocked: items.some((item) => item.state !== 'ready'),
    readyIds: items.filter((item) => item.state === 'ready').map((item) => item.stored.id),
  }
}
