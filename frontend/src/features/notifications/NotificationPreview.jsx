import { useState } from 'react'
import { Link } from 'react-router'
import { Popover } from 'radix-ui'
import { Bell, X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { StatePanel } from '@/components/StatePanel'
import { errorMessage } from '@/lib/errors'
import { formatRelative } from '@/i18n/format'
import { formatNumber } from '@/i18n/format'
import { useNotifications, useNotificationActions } from './use-notifications'

export function NotificationPreview() {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const { feed, count, items } = useNotifications(open)
  const { action, notice } = useNotificationActions(() => setOpen(false))
  const badge = count.data?.count > 99 ? '99+' : formatNumber(count.data?.count || 0)
  return <Popover.Root open={open} onOpenChange={setOpen}><Popover.Trigger asChild><Button variant="ghost" size="icon" className="icon-button notification-preview-trigger" aria-label={count.data?.count > 0 ? t('notifications:quickViewUnread', { badge, count: count.data.count }) : t('notifications:quickView')}>
    <Bell size={20} />{count.data?.count > 0 && <span className="preview-count" aria-hidden="true">{badge}</span>}
  </Button></Popover.Trigger><Popover.Portal><Popover.Content className="notification-preview" sideOffset={10} align="end" collisionPadding={12} aria-labelledby="notification-preview-title">
    <header><h2 id="notification-preview-title">{t('notifications:title')}</h2><Popover.Close asChild><Button variant="ghost" size="icon" aria-label={t('common:close')}><X size={18} /></Button></Popover.Close></header>
    {count.data && <p className="field-help">{t('notifications:unread', { count: count.data.count })}</p>}
    {(action.error || count.error) && <p role="alert" className="field-error">{errorMessage(action.error || count.error)}</p>}{notice && <p role="status" className="field-help">{t('notifications:readSuccess')}</p>}
    {feed.isPending ? <StatePanel compact loading /> : feed.error ? <StatePanel compact error={feed.error} onRetry={feed.refetch} /> : !items.length ? <p className="preview-empty">{t('notifications:emptyDescription')}</p> : <ul className="notification-preview-list">{items.slice(0, 5).map((item) => <li key={item.id} className={item.readAt ? '' : 'unread'}>
      <strong>{t(`notifications:types.${item.type}`, { defaultValue: t('notifications:types.unknown') })}</strong><time dateTime={item.createdAt}>{formatRelative(item.createdAt)}</time>{item.text && <p>{item.text}</p>}
      <div>{!item.readAt && <Button variant="ghost" disabled={action.isPending} onClick={() => action.mutate({ notification: item })}>{t('notifications:read')}</Button>}{item.referenceId && ['Message', 'GroupMessage', 'MessageRequest'].includes(item.type) && <Button variant="link" disabled={action.isPending} onClick={() => action.mutate({ notification: item, open: true })}>{t('notifications:view')}</Button>}</div>
    </li>)}</ul>}
    <footer><Button variant="ghost" disabled={action.isPending || !count.data?.count} onClick={() => action.mutate({ all: true })}>{t('notifications:readAll')}</Button><Link to="/notifications" className="text-link" onClick={() => setOpen(false)}>{t('notifications:viewAll')}</Link></footer>
  </Popover.Content></Popover.Portal></Popover.Root>
}
