import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Download, File, Image as ImageIcon, LoaderCircle, Paperclip, X } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
import { formatNumber } from '@/i18n/format'
import { filesApi } from './api'
import { FILE_ACCEPT } from './validation'

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
export function AttachmentDraft({ upload, disabled }) {
  const { t } = useTranslation()
  return <><div className="attachment-tools"><label className={`attach-file-button ${disabled ? 'disabled' : ''}`}><Paperclip size={17} /><span>{t('files:attach')}</span><input type="file" multiple accept={FILE_ACCEPT} className="sr-only" disabled={disabled || upload.uploading || upload.items.length >= 5} onChange={(event) => { upload.add(event.target.files); event.target.value = '' }} /></label><span>{t('files:countHint')}</span></div>{upload.validationError && <p className="field-error" role="alert">{upload.validationError}</p>}{upload.items.length > 0 && <ul className="attachment-drafts">{upload.items.map((item) => <li key={item.key}><File size={18} /><div><strong>{item.file.name}</strong><span>{item.state === 'uploading' ? t('files:uploadProgress', { progress: item.progress ?? 0 }) : item.state === 'ready' ? t('files:ready') : errorMessage(item.error)}</span></div>{item.state === 'uploading' && <LoaderCircle size={16} className="spin" />}{item.state === 'error' && <Button type="button" variant="link" disabled={disabled} onClick={() => upload.retry(item)}>{t('common:retry')}</Button>}<Button type="button" variant="ghost" size="icon" disabled={disabled} aria-label={t('files:remove', { name: item.file.name })} onClick={() => upload.remove(item.key)}><X size={15} /></Button></li>)}</ul>}</>
}
export function AttachmentPreview({ file }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const [downloading, setDownloading] = useState(false)
  const [error, setError] = useState(null)
  const preview = useQuery({ queryKey: ['workspace', 'file', file.id], queryFn: ({ signal }) => filesApi.preview(file.id, signal), enabled: open, gcTime: 0 })
  const url = useBlobUrl(open ? preview.data : null)
  const image = /^image\/(png|jpeg|webp)$/.test(file.contentType)
  async function download() {
    setDownloading(true); setError(null)
    try {
      const blob = await filesApi.download(file.id)
      const next = URL.createObjectURL(blob)
      const link = document.createElement('a'); link.href = next; link.download = file.fileName; link.click()
      setTimeout(() => URL.revokeObjectURL(next), 1000)
    } catch (error) { setError(error) } finally { setDownloading(false) }
  }
  return <Dialog open={open} onOpenChange={setOpen}><DialogTrigger asChild><button type="button" className="attachment-card">{image ? <ImageIcon size={20} /> : <File size={20} />}<span><strong>{file.fileName}</strong><small>{formatNumber(file.sizeBytes / 1024, { maximumFractionDigits: 1 })} {t('files:kib')}</small></span></button></DialogTrigger><DialogContent className="file-preview-dialog"><DialogHeader><DialogTitle>{file.fileName}</DialogTitle><DialogDescription>{t('files:previewDescription')}</DialogDescription></DialogHeader>{preview.isPending ? <StatePanel loading /> : preview.error ? <StatePanel error={preview.error} onRetry={preview.refetch} /> : image && url ? <img className="attachment-image" src={url} alt={file.fileName} /> : file.contentType === 'text/plain' && preview.data ? <TextPreview blob={preview.data} /> : file.contentType === 'application/pdf' && url ? <a className="text-link" href={url} target="_blank" rel="noopener noreferrer">{t('files:openPdf')}</a> : <div className="file-preview-placeholder"><File size={44} /><p>{t('files:noInlinePreview')}</p></div>}{error && <p className="field-error" role="alert">{errorMessage(error)}</p>}<Button disabled={downloading} onClick={download}><Download size={17} />{t(downloading ? 'files:downloading' : 'files:download')}</Button></DialogContent></Dialog>
}
function TextPreview({ blob }) {
  const { t } = useTranslation()
  const [text, setText] = useState('')
  useEffect(() => { let active = true; blob.slice(0, 100000).text().then((text) => { if (active) setText(text) }); return () => { active = false } }, [blob])
  return <><pre className="text-file-preview">{text}</pre>{blob.size > 100000 && <p className="field-help" role="status">{t('files:partialText')}</p>}</>
}
