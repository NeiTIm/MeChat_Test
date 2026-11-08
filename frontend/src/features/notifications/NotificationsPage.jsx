import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Bell, CheckCheck, MessageSquare, ShieldCheck } from 'lucide-react'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
import { queryClient } from '@/lib/query-client'
import { formatDate, formatTime, formatNumber } from '@/i18n/format'
import { notificationsApi } from './api'
import { useNotifications, useNotificationActions, useNotificationLifecycle } from './use-notifications'
import { ConfirmAction } from '@/components/ConfirmAction'
import { bcrFeatures } from '@/lib/bcr-features'
import { showSuccess } from '@/stores/ui-feedback-store'

export function NotificationsPage() {
  const { t } = useTranslation()
  const [trash, setTrash] = useState(false)
  const { feed: notifications, count, items } = useNotifications(true, trash)
  const { action, notice } = useNotificationActions()
  const lifecycle = useNotificationLifecycle()
  const pending = action.isPending || lifecycle.isPending
  return <ApplicationLayout area="notifications" title={t('notifications:title')} description={t('notifications:description')} actions={!trash && <Button variant="outline" disabled={pending || !count.data?.count} onClick={() => action.mutate({ all: true })}><CheckCheck size={17} /><span>{t('notifications:readAll')}</span></Button>}>
    {bcrFeatures.notifications && <div className="notification-toolbar">
      <nav className="notification-tabs" aria-label={t('notifications:views')}>
        {[false, true].map(value => <Button key={String(value)} variant={trash === value ? 'default' : 'outline'} aria-pressed={trash === value} onClick={() => { setTrash(value); lifecycle.reset() }}>{t(value ? 'notifications:trash' : 'notifications:active')}</Button>)}
      </nav>
      {!trash && <ConfirmAction destructive disabled={pending} label={t('notifications:deleteRead')} description={t('notifications:deleteReadConfirm')} onConfirm={() => lifecycle.mutateAsync({ method: 'deleteRead' })} />}
    </div>}
    {count.data && <p className="notification-count">{t('notifications:unread', { count: formatNumber(count.data.count) })}</p>}
    {(action.error || count.error || lifecycle.error) && <p role="alert" className="form-feedback feedback-error">{errorMessage(action.error || count.error || lifecycle.error)}</p>}
    {notice && <p className="form-feedback feedback-success" role="status">{t('notifications:readSuccess')}</p>}
    {lifecycle.isSuccess && <p className="form-feedback feedback-success" role="status">{t('notifications:lifecycleSuccess')}</p>}
    {notifications.isPending ? <StatePanel loading /> : notifications.error ? <StatePanel error={notifications.error} onRetry={notifications.refetch} /> : !items.length ? <section className="people-empty"><Bell /><StatePanel title={t(notifications.hasNextPage ? 'notifications:scanEmpty' : trash ? 'notifications:trashEmpty' : 'notifications:empty')} description={t(notifications.hasNextPage ? 'notifications:scanDescription' : trash ? 'notifications:trashDescription' : 'notifications:emptyDescription')} /></section> : <div className="notification-list">{items.map(item => <article key={item.id} className={`notification-card ${item.readAt ? '' : 'unread'}`}>
      <span className="notification-icon">{item.type === 'AdminWarning' ? <ShieldCheck /> : <MessageSquare />}</span><div>
        <h2>{t(`notifications:types.${item.type}`, { defaultValue: t('notifications:types.unknown') })}</h2>
        <time dateTime={item.createdAt}>{formatDate(item.createdAt)} · {formatTime(item.createdAt)}</time>
        {item.text && <p>{item.text}</p>}
        {trash && <p className="field-help">{t('notifications:deletedAt', { date: `${formatDate(item.deletedAt)} · ${formatTime(item.deletedAt)}` })}</p>}
        <div className="notification-actions">
          {trash ? <Button variant="outline" disabled={pending} onClick={() => lifecycle.mutate({ method: 'restore', id: item.id })}>{t('notifications:restore')}</Button> : <>
            {!item.readAt && <Button variant="ghost" disabled={pending} onClick={() => action.mutate({ notification: item })}>{t('notifications:read')}</Button>}
            {item.referenceId && ['Message', 'GroupMessage', 'MessageRequest'].includes(item.type) && <Button variant="outline" disabled={pending} onClick={() => action.mutate({ notification: item, open: true })}>{t('notifications:view')}</Button>}
            {bcrFeatures.notifications && <Button variant="outline" disabled={pending} onClick={() => lifecycle.mutate({ method: 'remove', id: item.id })}>{t('notifications:delete')}</Button>}
          </>}
        </div>
      </div>{!item.readAt && <span className="notification-unread-dot"><span className="sr-only">{t('notifications:newLabel')}</span></span>}
    </article>)}</div>}
    {notifications.hasNextPage && <Button className="load-more-people" variant="outline" disabled={notifications.isFetchingNextPage} onClick={() => notifications.fetchNextPage()}>{notifications.isFetchingNextPage ? t('common:saving') : t('common:loadMore')}</Button>}
  </ApplicationLayout>
}
export function ConversationNotifications({ id }) {
  const { t } = useTranslation()
  const [hours, setHours] = useState('1')
  const settings = useQuery({ queryKey: ['workspace', 'conversationSettings', id], queryFn: ({ signal }) => notificationsApi.settings(id, signal) })
  const action = useMutation({ mutationFn: (muted) => notificationsApi.mute(id, muted ? new Date(Date.now() + Number(hours) * 3600000).toISOString() : null), onSuccess: () => { showSuccess(t('notifications:saved')); return queryClient.invalidateQueries({ queryKey: ['workspace', 'conversationSettings', id] }) } })
  return <section className="conversation-notifications"><h3>{t('notifications:settingsTitle')}</h3><p className="field-help">{t('notifications:settingsDescription')}</p>{settings.isPending ? <StatePanel compact loading /> : settings.error ? <StatePanel compact error={settings.error} onRetry={settings.refetch} /> : <><p className="field-help">{settings.data.mutedUntil && Date.parse(settings.data.mutedUntil) > Date.now() ? t('notifications:mutedUntil', { date: `${formatDate(settings.data.mutedUntil)} · ${formatTime(settings.data.mutedUntil)}` }) : t('notifications:notMuted')}</p><label htmlFor="mute-duration">{t('notifications:duration')}</label><select id="mute-duration" value={hours} disabled={action.isPending} onChange={(event) => setHours(event.target.value)}>{['1', '8', '24', '168'].map((value) => <option value={value} key={value}>{t(`notifications:durations.${value}`)}</option>)}</select><Button variant="outline" disabled={action.isPending} onClick={() => action.mutate(true)}>{t('notifications:mute')}</Button>{settings.data.mutedUntil && <Button variant="ghost" disabled={action.isPending} onClick={() => action.mutate(false)}>{t('notifications:unmute')}</Button>}</>}{action.error && <p role="alert" className="field-error">{errorMessage(action.error)}</p>}{action.isSuccess && <p role="status" className="field-help">{t('notifications:saved')}</p>}</section>
}
