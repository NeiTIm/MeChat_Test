import { useTranslation } from 'react-i18next'
import { useQuery } from '@tanstack/react-query'
import { z } from 'zod'
import { Flag } from 'lucide-react'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { statusLabel } from '@/i18n'
import { formatDate } from '@/i18n/format'
import { queryClient } from '@/lib/query-client'
import { reportsApi } from './api'

export function ReportForm({ targetType, targetId }) {
  const { t } = useTranslation()
  return <ValidatedForm id={`report-${targetId}`} fields={[{ name: 'reason', label: t('reports:reason'), type: 'textarea', maxLength: 1000 }]} schema={z.object({ reason: z.string().trim().min(1, t('common:required')).max(1000, t('reports:reasonLimit')) })} onSubmit={async ({ reason }) => { await reportsApi.create({ targetType, targetId, reason }); await queryClient.invalidateQueries({ queryKey: ['workspace', 'reports'] }) }} submitLabel={t('reports:submit')} successMessage={t('reports:submitted')} resetOnSuccess />
}
export function ReportDialog({ targetType, targetId }) {
  const { t } = useTranslation()
  return <Dialog><DialogTrigger asChild><Button variant="outline"><Flag size={16} />{t('reports:report')}</Button></DialogTrigger><DialogContent className="details-dialog"><DialogHeader><DialogTitle>{t('reports:report')}</DialogTitle><DialogDescription>{t('reports:reportDescription')}</DialogDescription></DialogHeader><ReportForm targetType={targetType} targetId={targetId} /></DialogContent></Dialog>
}
export function ReportsPage() {
  const { t } = useTranslation()
  const reports = useQuery({ queryKey: ['workspace', 'reports'], queryFn: reportsApi.mine })
  return <ApplicationLayout area="account" title={t('reports:title')} description={t('reports:description')}>
    {reports.isPending ? <StatePanel loading /> : reports.error ? <StatePanel error={reports.error} onRetry={reports.refetch} /> : !reports.data.length ? <section className="settings-card"><StatePanel title={t('reports:empty')} /></section> : <div className="notification-list">{reports.data.map((report) => <article className="settings-card report-card" key={report.id}><header><h2>{t(`reports:targets.${report.targetType}`)}</h2><span className="role-chip">{statusLabel(report.status)}</span></header><p>{report.reason}</p><p className="field-help">{t('reports:created', { date: formatDate(report.createdAt) })}</p>{report.reviewedAt && <p className="field-help">{t('reports:reviewed', { date: formatDate(report.reviewedAt) })}</p>}</article>)}</div>}
  </ApplicationLayout>
}
