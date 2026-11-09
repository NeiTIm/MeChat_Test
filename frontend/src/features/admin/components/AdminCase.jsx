import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Download } from 'lucide-react'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { statusLabel } from '@/i18n'
import { formatDate, formatTime } from '@/i18n/format'
import { errorMessage } from '@/lib/errors'
import { AdminAction, HoldAction } from '@/features/admin/AdminActions'
import { adminApi } from '@/features/admin/api'

export function AdminCase({ id }) {
  const { t } = useTranslation()
  const report = useQuery({
    queryKey: ['workspace', 'admin', 'case', id],
    queryFn: ({ signal }) => adminApi.case(id, signal),
  })
  const download = useMutation({
    mutationFn: async (evidenceId) => {
      const result = await adminApi.evidence(id, evidenceId)
      const url = URL.createObjectURL(result.blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = result.fileName
      anchor.click()
      setTimeout(() => URL.revokeObjectURL(url), 1000)
    },
  })
  if (report.isPending) return <StatePanel loading />
  if (report.error)
    return (
      <StatePanel
        error={report.error}
        onRetry={report.refetch}
      />
    )
  const { report: item, evidence } = report.data
  return (
    <>
      <section className="settings-card">
        <h2>{t('admin:caseTitle')}</h2>
        <p className="page-description">{t('admin:caseDescription')}</p>
        <span className="role-chip">{statusLabel(item.status)}</span>
        <p className="case-reason">{item.reason}</p>
        <dl className="audit-details">
          <div>
            <dt>{t('admin:reporter')}</dt>
            <dd>{item.reporterUserId}</dd>
          </div>
          <div>
            <dt>{t('admin:target')}</dt>
            <dd>
              {t(`reports:targets.${item.targetType}`)} · {item.targetId}
            </dd>
          </div>
        </dl>
        {item.status === 'Pending' && (
          <div className="section-actions">
            {['Resolved', 'Dismissed'].map((status) => (
              <AdminAction
                key={status}
                label={t(status === 'Resolved' ? 'admin:resolve' : 'admin:dismiss')}
                field="note"
                target={id}
                description={t('admin:resolveDescription')}
                onConfirm={({ note }) => adminApi.resolve(id, { status, note })}
              />
            ))}
          </div>
        )}
      </section>
      <section className="settings-card">
        <h2>{t('admin:evidence')}</h2>
        {download.error && (
          <p
            role="alert"
            className="field-error"
          >
            {errorMessage(download.error)}
          </p>
        )}
        {!evidence.length ? (
          <StatePanel title={t('admin:noEvidence')} />
        ) : (
          evidence.map((entry) => (
            <article
              className="evidence-card"
              key={entry.id}
            >
              <time>
                {formatDate(entry.capturedAt)} · {formatTime(entry.capturedAt)}
              </time>
              <p>{entry.evidenceContent || t('admin:evidenceMissing')}</p>
              <div className="section-actions">
                {entry.storedFileId && (
                  <Button
                    variant="outline"
                    disabled={download.isPending}
                    onClick={() => download.mutate(entry.id)}
                  >
                    <Download size={16} />
                    {t('admin:evidenceFile')}
                  </Button>
                )}
                <HoldAction
                  kind="Evidence"
                  id={entry.id}
                  held={entry.legalHold}
                />
              </div>
            </article>
          ))
        )}
      </section>
    </>
  )
}
