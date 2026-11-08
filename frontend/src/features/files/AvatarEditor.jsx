import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
import { filesApi } from './api'
import { validateUpload } from './validation'
import { useBlobUrl } from './AttachmentPreview'
import { showSuccess } from '@/stores/ui-feedback-store'
import { queryClient } from '@/lib/query-client'

export function AvatarEditor({ onSave, group = false }) {
  const { t } = useTranslation()
  const [file, setFile] = useState(null)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)
  const [progress, setProgress] = useState(0)
  const controllerRef = useRef(null)
  useEffect(() => () => controllerRef.current?.abort(), [])
  const url = useBlobUrl(file)
  async function save() {
    if (!file || pending) return
    const controller = new AbortController()
    controllerRef.current = controller
    setPending(true); setError(''); setSaved(false)
    try { const uploaded = await filesApi.upload(file, 'Avatar', controller.signal, setProgress); await onSave(uploaded.id); await queryClient.invalidateQueries({ queryKey: ['workspace'] }); setFile(null); setSaved(true); showSuccess(t('files:avatarSaved')) }
    catch (error) { if (!controller.signal.aborted) setError(errorMessage(error)) }
    finally { if (!controller.signal.aborted) setPending(false) }
  }
  return <div className="avatar-editor"><h3>{t(group ? 'files:groupAvatar' : 'files:avatarTitle')}</h3><p className="field-help">{t('files:avatarDescription')}</p>{url && <img src={url} alt={t('files:avatarTitle')} />}<label className="attach-file-button"><span>{t('files:chooseAvatar')}</span><input type="file" className="sr-only" accept=".jpg,.jpeg,.png,.webp" disabled={pending} onChange={(event) => { const next = event.target.files[0]; event.target.value = ''; if (!next) return; setSaved(false); const invalid = validateUpload(next, 'Avatar'); setError(invalid || ''); setFile(invalid ? null : next) }} /></label><Button disabled={!file || pending} onClick={save}>{pending ? t('files:uploadProgress', { progress }) : t('files:saveAvatar')}</Button>{error && <p className="field-error" role="alert">{error}</p>}{saved && <p className="field-help" role="status">{t('files:avatarSaved')}</p>}</div>
}
