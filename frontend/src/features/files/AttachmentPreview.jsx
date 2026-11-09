import { useBlobUrl } from '@/features/files/hooks/use-blob-url'
import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Download, File, Image as ImageIcon, ZoomIn, Minimize2 } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
import { formatNumber } from '@/i18n/format'
import { filesApi } from './api'

export { useBlobUrl } from '@/features/files/hooks/use-blob-url'
export { AttachmentDraft } from '@/features/files/components/AttachmentDraft'
export function AttachmentPreview({ file }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const [downloading, setDownloading] = useState(false)
  const [error, setError] = useState(null)
  const [zoom, setZoom] = useState(false)
  const preview = useQuery({
    queryKey: ['workspace', 'file', file.id],
    queryFn: ({ signal }) => filesApi.preview(file.id, signal),
    enabled: open,
    gcTime: 0,
  })
  const url = useBlobUrl(open ? preview.data : null)
  const image = /^image\/(png|jpeg|webp)$/.test(file.contentType)
  async function download() {
    setDownloading(true)
    setError(null)
    try {
      const blob = await filesApi.download(file.id)
      const next = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = next
      link.download = file.fileName
      link.click()
      setTimeout(() => URL.revokeObjectURL(next), 1000)
    } catch (error) {
      setError(error)
    } finally {
      setDownloading(false)
    }
  }
  return (
    <Dialog
      open={open}
      onOpenChange={(value) => {
        setOpen(value)
        setZoom(false)
        setError(null)
      }}
    >
      <DialogTrigger asChild>
        <button
          type="button"
          className={`attachment-card ${image ? 'image-attachment' : ''}`}
        >
          <span className="attachment-caption">
            {image ? <ImageIcon size={20} /> : <File size={20} />}
            <span>
              <strong>{file.fileName}</strong>
              <small>
                {formatNumber(file.sizeBytes / 1024, { maximumFractionDigits: 1 })} {t('files:kib')}
              </small>
            </span>
          </span>
          {image && <AttachmentThumbnail file={file} />}
        </button>
      </DialogTrigger>
      <DialogContent className="file-preview-dialog">
        <DialogHeader>
          <DialogTitle>{file.fileName}</DialogTitle>
          <DialogDescription>{t('files:previewDescription')}</DialogDescription>
        </DialogHeader>
        {preview.isPending ? (
          <StatePanel loading />
        ) : preview.error ? (
          <StatePanel
            error={preview.error}
            onRetry={preview.refetch}
          />
        ) : image && preview.data ? (
          <>
            <ImagePreview
              key={url}
              blob={preview.data}
              url={url}
              name={file.fileName}
              zoom={zoom}
              onZoom={() => setZoom(!zoom)}
              onRetry={preview.refetch}
            />
          </>
        ) : file.contentType === 'text/plain' && preview.data ? (
          <TextPreview blob={preview.data} />
        ) : file.contentType === 'application/pdf' && url ? (
          <a
            className="text-link"
            href={url}
            target="_blank"
            rel="noopener noreferrer"
          >
            {t('files:openPdf')}
          </a>
        ) : (
          <div className="file-preview-placeholder">
            <File size={44} />
            <p>{t('files:noInlinePreview')}</p>
          </div>
        )}
        {error && (
          <p
            className="field-error"
            role="alert"
          >
            {errorMessage(error)}
          </p>
        )}
        <Button
          disabled={downloading}
          onClick={download}
        >
          <Download size={17} />
          {t(downloading ? 'files:downloading' : 'files:download')}
        </Button>
      </DialogContent>
    </Dialog>
  )
}

const isImageBlob = (blob) =>
  blob?.size > 0 && /^image\/(png|jpeg|webp)$/i.test(blob.type.split(';')[0].trim())
function ImagePreview({ blob, url, name, zoom, onZoom, onRetry }) {
  const { t } = useTranslation()
  const [failed, setFailed] = useState(false)
  if (!isImageBlob(blob) || failed)
    return (
      <div className="image-preview-error">
        <p
          role="alert"
          className="field-error"
        >
          {t('files:imageUnavailable')}
        </p>
        <Button
          variant="outline"
          onClick={onRetry}
        >
          {t('common:retry')}
        </Button>
      </div>
    )
  return url ? (
    <>
      <div
        className={`image-preview-viewport ${zoom ? 'image-zoomed' : ''}`}
        tabIndex={zoom ? 0 : undefined}
        aria-label={zoom ? t('files:panImage') : undefined}
      >
        <img
          className="attachment-image"
          src={url}
          alt={name}
          decoding="async"
          onError={() => setFailed(true)}
        />
      </div>
      <Button
        variant="outline"
        onClick={onZoom}
        aria-pressed={zoom}
      >
        {zoom ? <Minimize2 size={17} /> : <ZoomIn size={17} />}
        {t(zoom ? 'files:fitImage' : 'files:zoomImage')}
      </Button>
    </>
  ) : (
    <StatePanel loading />
  )
}
function AttachmentThumbnail({ file }) {
  const { t } = useTranslation()
  const ref = useRef(null)
  const [visible, setVisible] = useState(false)
  const [failedUrl, setFailedUrl] = useState(null)
  useEffect(() => {
    if (!ref.current) return
    if (!window.IntersectionObserver) {
      setVisible(true)
      return
    }
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setVisible(true)
          observer.disconnect()
        }
      },
      { rootMargin: '160px' },
    )
    observer.observe(ref.current)
    return () => observer.disconnect()
  }, [])
  const preview = useQuery({
    queryKey: ['workspace', 'file', file.id],
    queryFn: ({ signal }) => filesApi.preview(file.id, signal),
    enabled: visible,
    gcTime: 0,
  })
  const url = useBlobUrl(preview.data)
  const unavailable =
    preview.error || (preview.data && !isImageBlob(preview.data)) || (url && failedUrl === url)
  return (
    <span
      ref={ref}
      className="attachment-thumbnail"
    >
      {unavailable ? (
        <span>{t('files:thumbnailUnavailable')}</span>
      ) : url ? (
        <img
          src={url}
          alt=""
          loading="lazy"
          decoding="async"
          onError={() => setFailedUrl(url)}
        />
      ) : (
        <span>{t('files:loadingImage')}</span>
      )}
    </span>
  )
}
function TextPreview({ blob }) {
  const { t } = useTranslation()
  const [text, setText] = useState('')
  useEffect(() => {
    let active = true
    blob
      .slice(0, 100000)
      .text()
      .then((text) => {
        if (active) setText(text)
      })
    return () => {
      active = false
    }
  }, [blob])
  return (
    <>
      <pre className="text-file-preview">{text}</pre>
      {blob.size > 100000 && (
        <p
          className="field-help"
          role="status"
        >
          {t('files:partialText')}
        </p>
      )}
    </>
  )
}
