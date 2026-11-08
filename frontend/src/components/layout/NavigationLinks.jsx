import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { useTranslation } from 'react-i18next'
import { useAuthStore } from '@/stores/auth-store'
import { messagesApi } from '@/features/messages/api'
import { notificationsApi } from '@/features/notifications/api'
import { formatNumber } from '@/i18n/format'

export function NavigationLinks({ items, mobile = false, preview = false }) {
  const userId = useAuthStore((state) => state.access?.userId)
  return userId && !preview ? <ServerNavigationLinks items={items} mobile={mobile} /> : <NavigationItems items={items} mobile={mobile} />
}
function ServerNavigationLinks({ items, mobile }) {
  const summary = useQuery({ queryKey: ['workspace', 'unreadSummary'], queryFn: messagesApi.summary })
  const notifications = useQuery({ queryKey: ['workspace', 'notificationCount'], queryFn: notificationsApi.count })
  // These entries are the complete server projection, not the rendered inbox rows.
  const total = (type) => summary.data?.conversations.filter((entry) => entry.type === type).reduce((sum, entry) => sum + entry.count, 0)
  const privateCount = total('Private')
  const counts = { '/chat': privateCount === undefined ? undefined : privateCount + summary.data.requestCount, '/groups': total('Group'), '/notifications': notifications.data?.count }
  return <NavigationItems items={items} mobile={mobile} counts={counts} />
}
function NavigationItems({ items, mobile, counts = {} }) {
  const { t } = useTranslation()
  return items.map(({ label, to, Icon, active }) => <Link key={to} className={`${mobile ? '' : 'rail-link'} ${active ? 'active' : ''}`} to={to} aria-current={active ? 'page' : undefined} title={label}>
    <span className="nav-icon"><Icon size={mobile ? 20 : 22} />{counts[to] > 0 && <span className="nav-badge" aria-label={t('common:unreadCount', { count: counts[to] })}>{counts[to] > 99 ? '99+' : formatNumber(counts[to])}</span>}</span><span>{label}</span>
  </Link>)
}
