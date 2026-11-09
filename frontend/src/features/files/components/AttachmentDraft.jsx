import { useTranslation } from 'react-i18next'
import { File, LoaderCircle, Paperclip, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
import { FILE_ACCEPT } from '@/features/files/validation'

export function AttachmentDraft({ upload, disabled }) {
  const { t } = useTranslation()
  return (
    <>
      <div className="attachment-tools">
        <label className={`attach-file-button ${disabled ? 'disabled' : ''}`}>
          <Paperclip size={17} />
          <span>{t('files:attach')}</span>
          <input
            type="file"
            multiple
            accept={FILE_ACCEPT}
            className="sr-only"
            disabled={disabled || upload.uploading || upload.items.length >= 5}
            onChange={(event) => {
              upload.add(event.target.files)
              event.target.value = ''
            }}
          />
        </label>
        <span>{t('files:countHint')}</span>
      </div>
      {upload.validationError && (
        <p
          className="field-error"
          role="alert"
        >
          {upload.validationError}
        </p>
      )}
      {upload.items.length > 0 && (
        <ul className="attachment-drafts">
          {upload.items.map((item) => (
            <li key={item.key}>
              <File size={18} />
              <div>
                <strong>{item.file.name}</strong>
                <span>
                  {item.state === 'uploading'
                    ? t('files:uploadProgress', { progress: item.progress ?? 0 })
                    : item.state === 'ready'
                      ? t('files:ready')
                      : errorMessage(item.error)}
                </span>
              </div>
              {item.state === 'uploading' && (
                <LoaderCircle
                  size={16}
                  className="spin"
                />
              )}
              {item.state === 'error' && (
                <Button
                  type="button"
                  variant="link"
                  disabled={disabled}
                  onClick={() => upload.retry(item)}
                >
                  {t('common:retry')}
                </Button>
              )}
              <Button
                type="button"
                variant="ghost"
                size="icon"
                disabled={disabled}
                aria-label={t('files:remove', { name: item.file.name })}
                onClick={() => upload.remove(item.key)}
              >
                <X size={15} />
              </Button>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
