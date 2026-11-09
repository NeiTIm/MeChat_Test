import { MessageSquare, ShieldCheck } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { formatDate, formatTime } from '@/i18n/format'
import { bcrFeatures } from '@/lib/bcr-features'

export function NotificationCard({ t, item, trash, pending, lifecycle, action }) {
  return (
    <article
      key={item.id}
      className={`notification-card ${item.readAt ? '' : 'unread'}`}
    >
      <span className="notification-icon">
        {item.type === 'AdminWarning' ? <ShieldCheck /> : <MessageSquare />}
      </span>
      <div>
        <h2>
          {t(`notifications:types.${item.type}`, {
            defaultValue: t('notifications:types.unknown'),
          })}
        </h2>
        <time dateTime={item.createdAt}>
          {formatDate(item.createdAt)} · {formatTime(item.createdAt)}
        </time>
        {item.text && <p>{item.text}</p>}
        {trash && (
          <p className="field-help">
            {t('notifications:deletedAt', {
              date: `${formatDate(item.deletedAt)} · ${formatTime(item.deletedAt)}`,
            })}
          </p>
        )}
        <div className="notification-actions">
          {trash ? (
            <Button
              variant="outline"
              disabled={pending}
              onClick={() => lifecycle.mutate({ method: 'restore', id: item.id })}
            >
              {t('notifications:restore')}
            </Button>
          ) : (
            <>
              {!item.readAt && (
                <Button
                  variant="ghost"
                  disabled={pending}
                  onClick={() => action.mutate({ notification: item })}
                >
                  {t('notifications:read')}
                </Button>
              )}
              {item.referenceId &&
                ['Message', 'GroupMessage', 'MessageRequest'].includes(item.type) && (
                  <Button
                    variant="outline"
                    disabled={pending}
                    onClick={() => action.mutate({ notification: item, open: true })}
                  >
                    {t('notifications:view')}
                  </Button>
                )}
              {bcrFeatures.notifications && (
                <Button
                  variant="outline"
                  disabled={pending}
                  onClick={() => lifecycle.mutate({ method: 'remove', id: item.id })}
                >
                  {t('notifications:delete')}
                </Button>
              )}
            </>
          )}
        </div>
      </div>
      {!item.readAt && (
        <span className="notification-unread-dot">
          <span className="sr-only">{t('notifications:newLabel')}</span>
        </span>
      )}
    </article>
  )
}
