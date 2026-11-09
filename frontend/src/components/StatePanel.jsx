import { useTranslation } from 'react-i18next'
import { AlertCircle, LoaderCircle, MessageSquare, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
export function StatePanel({ loading, error, title, description, onRetry, compact }) {
  const { t } = useTranslation()
  return (
    <div
      className={`state-panel ${compact ? 'state-compact' : ''}`}
      role={error ? 'alert' : 'status'}
    >
      <span
        className="state-icon"
        aria-hidden="true"
      >
        {loading ? <LoaderCircle className="spin" /> : error ? <AlertCircle /> : <MessageSquare />}
      </span>
      <h2>
        {loading
          ? t('common:getting-things-ready')
          : error
            ? t('common:couldn-t-load-this')
            : title}
      </h2>
      <p>{error ? errorMessage(error) : description}</p>
      {error && onRetry && (
        <Button
          variant="outline"
          onClick={onRetry}
        >
          <RefreshCw />
          {t('common:try-again')}
        </Button>
      )}
    </div>
  )
}
