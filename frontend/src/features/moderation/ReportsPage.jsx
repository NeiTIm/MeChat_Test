import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'
import { statusLabel } from '@/i18n'
import { formatDate } from '@/i18n/format'
import { reportsApi } from './api'

export { ReportForm } from '@/features/moderation/components/ReportForm'
export { ReportDialog } from '@/features/moderation/components/ReportDialog'
export function ReportsPage() {
  const { t } = useTranslation()
  const reports = useQuery({ queryKey: ['workspace', 'reports'], queryFn: reportsApi.mine })
  return (
    <ApplicationLayout
      area="account"
      title={t('reports:title')}
      description={t('reports:description')}
    >
      {reports.isPending ? (
        <StatePanel loading />
      ) : reports.error ? (
        <StatePanel
          error={reports.error}
          onRetry={reports.refetch}
        />
      ) : !reports.data.length ? (
        <section className="settings-card">
          <StatePanel title={t('reports:empty')} />
        </section>
      ) : (
        <div className="notification-list">
          {reports.data.map((report) => (
            <article
              className="settings-card report-card"
              key={report.id}
            >
              <header>
                <h2>{t(`reports:targets.${report.targetType}`)}</h2>
                <span className="role-chip">{statusLabel(report.status)}</span>
              </header>
              <p>{report.reason}</p>
              <p className="field-help">
                {t('reports:created', { date: formatDate(report.createdAt) })}
              </p>
              {report.reviewedAt && (
                <p className="field-help">
                  {t('reports:reviewed', { date: formatDate(report.reviewedAt) })}
                </p>
              )}
            </article>
          ))}
        </div>
      )}
    </ApplicationLayout>
  )
}
