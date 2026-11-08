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
import { useNotifications, useNotificationActions } from './use-notifications'
import { showSuccess } from '@/stores/ui-feedback-store'

export function NotificationsPage() {
  const { t } = useTranslation()
  const { feed: notifications, count, items } = useNotifications()
  const { action, notice } = useNotificationActions()
  return <ApplicationLayout area="notifications" title={t('notifications:title')} description={t('notifications:description')} actions={<Button variant="outline" disabled={action.isPending || !count.data?.count} onClick={() => action.mutate({ all: true })}><CheckCheck size={17} /><span>{t('notifications:readAll')}</span></Button>}>
    {count.data && <p className="notification-count">{t('notifications:unread', { count: formatNumber(count.data.count) })}</p>}
    {(action.error || count.error) && <p role="alert" className="form-feedback feedback-error">{errorMessage(action.error || count.error)}</p>}{notice && <p className="form-feedback feedback-success" role="status">{t('notifications:readSuccess')}</p>}
    {notifications.isPending ? <StatePanel loading /> : notifications.error ? <StatePanel error={notifications.error} onRetry={notifications.refetch} /> : !items.length ? <section className="people-empty"><Bell /><StatePanel title={t('notifications:empty')} description={t('notifications:emptyDescription')} /></section> : <div className="notification-list">{items.map((item) => <article key={item.id} className={`notification-card ${item.readAt ? '' : 'unread'}`}><span className="notification-icon">{item.type === 'AdminWarning' ? <ShieldCheck /> : <MessageSquare />}</span><div><h2>{t(`notifications:types.${item.type}`, { defaultValue: t('notifications:types.unknown') })}</h2><time dateTime={item.createdAt}>{formatDate(item.createdAt)} · {formatTime(item.createdAt)}</time>{item.text && <p>{item.text}</p>}<div className="notification-actions">{!item.readAt && <Button variant="ghost" disabled={action.isPending} onClick={() => action.mutate({ notification: item })}>{t('notifications:read')}</Button>}{item.referenceId && ['Message', 'GroupMessage', 'MessageRequest'].includes(item.type) && <Button variant="outline" disabled={action.isPending} onClick={() => action.mutate({ notification: item, open: true })}>{t('notifications:view')}</Button>}</div></div>{!item.readAt && <span className="notification-unread-dot"><span className="sr-only">{t('notifications:newLabel')}</span></span>}</article>)}</div>}
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
