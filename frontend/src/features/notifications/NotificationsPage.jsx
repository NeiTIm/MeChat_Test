import { NotificationCard } from './components/NotificationCard'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Bell, CheckCheck } from 'lucide-react'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
import { formatNumber } from '@/i18n/format'
import {
  useNotifications,
  useNotificationActions,
  useNotificationLifecycle,
} from './use-notifications'
import { ConfirmAction } from '@/components/ConfirmAction'
import { bcrFeatures } from '@/lib/bcr-features'

export function NotificationsPage() {
  const { t } = useTranslation()
  const [trash, setTrash] = useState(false)
  const { feed: notifications, count, items } = useNotifications(true, trash)
  const { action, notice } = useNotificationActions()
  const lifecycle = useNotificationLifecycle()
  const pending = action.isPending || lifecycle.isPending
  return (
    <ApplicationLayout
      area="notifications"
      title={t('notifications:title')}
      description={t('notifications:description')}
      actions={
        !trash && (
          <Button
            variant="outline"
            disabled={pending || !count.data?.count}
            onClick={() => action.mutate({ all: true })}
          >
            <CheckCheck size={17} />
            <span>{t('notifications:readAll')}</span>
          </Button>
        )
      }
    >
      {bcrFeatures.notifications && (
        <div className="notification-toolbar">
          <nav
            className="notification-tabs"
            aria-label={t('notifications:views')}
          >
            {[false, true].map((value) => (
              <Button
                key={String(value)}
                variant={trash === value ? 'default' : 'outline'}
                aria-pressed={trash === value}
                onClick={() => {
                  setTrash(value)
                  lifecycle.reset()
                }}
              >
                {t(value ? 'notifications:trash' : 'notifications:active')}
              </Button>
            ))}
          </nav>
          {!trash && (
            <ConfirmAction
              destructive
              disabled={pending}
              label={t('notifications:deleteRead')}
              description={t('notifications:deleteReadConfirm')}
              onConfirm={() => lifecycle.mutateAsync({ method: 'deleteRead' })}
            />
          )}
        </div>
      )}
      {count.data && (
        <p className="notification-count">
          {t('notifications:unread', { count: formatNumber(count.data.count) })}
        </p>
      )}
      {(action.error || count.error || lifecycle.error) && (
        <p
          role="alert"
          className="form-feedback feedback-error"
        >
          {errorMessage(action.error || count.error || lifecycle.error)}
        </p>
      )}
      {notice && (
        <p
          className="form-feedback feedback-success"
          role="status"
        >
          {t('notifications:readSuccess')}
        </p>
      )}
      {lifecycle.isSuccess && (
        <p
          className="form-feedback feedback-success"
          role="status"
        >
          {t('notifications:lifecycleSuccess')}
        </p>
      )}
      {notifications.isPending ? (
        <StatePanel loading />
      ) : notifications.error ? (
        <StatePanel
          error={notifications.error}
          onRetry={notifications.refetch}
        />
      ) : !items.length ? (
        <section className="people-empty">
          <Bell />
          <StatePanel
            title={t(
              notifications.hasNextPage
                ? 'notifications:scanEmpty'
                : trash
                  ? 'notifications:trashEmpty'
                  : 'notifications:empty',
            )}
            description={t(
              notifications.hasNextPage
                ? 'notifications:scanDescription'
                : trash
                  ? 'notifications:trashDescription'
                  : 'notifications:emptyDescription',
            )}
          />
        </section>
      ) : (
        <div className="notification-list">
          {items.map((item) => (
            <NotificationCard
              key={item.id}
              t={t}
              item={item}
              trash={trash}
              pending={pending}
              lifecycle={lifecycle}
              action={action}
            />
          ))}
        </div>
      )}
      {notifications.hasNextPage && (
        <Button
          className="load-more-people"
          variant="outline"
          disabled={notifications.isFetchingNextPage}
          onClick={() => notifications.fetchNextPage()}
        >
          {notifications.isFetchingNextPage ? t('common:saving') : t('common:loadMore')}
        </Button>
      )}
    </ApplicationLayout>
  )
}
export { ConversationNotifications } from '@/features/notifications/components/ConversationNotifications'
