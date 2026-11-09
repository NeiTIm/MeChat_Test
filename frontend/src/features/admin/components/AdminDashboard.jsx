import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { StatePanel } from '@/components/StatePanel'
import { formatDate, formatNumber } from '@/i18n/format'
import { adminApi } from '@/features/admin/api'
import { MetricCards } from './MetricCards'

export function AdminDashboard() {
  const { t } = useTranslation()
  const dashboard = useQuery({
    queryKey: ['workspace', 'admin', 'dashboard'],
    queryFn: adminApi.dashboard,
  })
  if (dashboard.isPending) return <StatePanel loading />
  if (dashboard.error)
    return (
      <StatePanel
        error={dashboard.error}
        onRetry={dashboard.refetch}
      />
    )
  const { activity, ...metrics } = dashboard.data
  const max = Math.max(1, ...activity.map((day) => day.messages))
  return (
    <>
      <MetricCards data={metrics} />
      <section className="settings-card admin-activity">
        <h2>{t('admin:activityTitle')}</h2>
        <p className="page-description">{t('admin:activityDescription')}</p>
        {!activity.length ? (
          <StatePanel title={t('admin:empty')} />
        ) : (
          <div className="activity-rows">
            <div className="activity-row activity-labels">
              <span>{t('admin:date')}</span>
              <span>{t('admin:newUsers')}</span>
              <span>{t('admin:messages')}</span>
            </div>
            {activity.map((day) => (
              <div
                className="activity-row"
                key={day.date}
              >
                <time>{formatDate(day.date)}</time>
                <strong>{formatNumber(day.newUsers)}</strong>
                <span className="activity-value">
                  <span
                    className="activity-bar"
                    style={{ width: `${Math.max(1, (day.messages / max) * 100)}%` }}
                  />
                  <strong>{formatNumber(day.messages)}</strong>
                </span>
              </div>
            ))}
          </div>
        )}
      </section>
    </>
  )
}
