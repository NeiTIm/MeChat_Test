import { useState } from 'react'
import { useInfiniteQuery, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { ShieldCheck } from 'lucide-react'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { formatDate, formatNumber } from '@/i18n/format'
import { HoldAction } from '@/features/admin/AdminActions'
import { adminApi } from '@/features/admin/api'
import { MetricCards } from './MetricCards'

export function AdminRetention() {
  const { t } = useTranslation()
  const [kind, setKind] = useState('Evidence')
  const [requestedKind, setRequestedKind] = useState(null)
  const retention = useQuery({
    queryKey: ['workspace', 'admin', 'retention'],
    queryFn: adminApi.retention,
  })
  const files = useQuery({
    queryKey: ['workspace', 'admin', 'fileRetention'],
    queryFn: adminApi.fileRetention,
  })
  const plan = useInfiniteQuery({
    queryKey: ['workspace', 'admin', 'retentionPlan', requestedKind],
    queryFn: ({ pageParam, signal }) => adminApi.plan(requestedKind, pageParam, signal),
    initialPageParam: 0,
    getNextPageParam: (page) => page.nextOffset ?? undefined,
    enabled: !!requestedKind,
  })
  return (
    <>
      <section className="retention-intro">
        <ShieldCheck />
        <div>
          <h2>{t('admin:retentionTitle')}</h2>
          <p>{t('admin:retentionDescription')}</p>
        </div>
      </section>
      {retention.isPending ? (
        <StatePanel loading />
      ) : retention.error ? (
        <StatePanel
          error={retention.error}
          onRetry={retention.refetch}
        />
      ) : (
        <MetricCards
          data={retention.data}
          namespace="admin:retentionMetrics"
        />
      )}
      <section className="settings-card">
        <h2>{t('admin:planTitle')}</h2>
        <p className="page-description">{t('admin:planDescription')}</p>
        <form
          className="retention-plan-controls"
          onSubmit={(event) => {
            event.preventDefault()
            if (kind === requestedKind) plan.refetch()
            else setRequestedKind(kind)
          }}
        >
          <label htmlFor="retention-kind">{t('admin:kind')}</label>
          <select
            id="retention-kind"
            value={kind}
            onChange={(event) => setKind(event.target.value)}
          >
            {['Evidence', 'Audit'].map((value) => (
              <option
                value={value}
                key={value}
              >
                {t(`admin:kinds.${value}`)}
              </option>
            ))}
          </select>
          <Button
            type="submit"
            disabled={plan.isFetching}
          >
            {t('admin:loadPlan')}
          </Button>
        </form>
        {requestedKind &&
          (plan.isPending ? (
            <StatePanel loading />
          ) : plan.error ? (
            <StatePanel
              error={plan.error}
              onRetry={plan.refetch}
            />
          ) : (
            <>
              {!plan.data.pages.some((page) => page.items.length) ? (
                <StatePanel title={t('admin:empty')} />
              ) : (
                plan.data.pages
                  .flatMap((page) => page.items)
                  .map((item) => (
                    <article
                      className="retention-plan-row"
                      key={item.id}
                    >
                      <div>
                        <strong>{item.id}</strong>
                        <p>{t('admin:eligibleSince', { date: formatDate(item.eligibleSince) })}</p>
                      </div>
                      {requestedKind === 'Audit' && (
                        <HoldAction
                          kind="Audit"
                          id={item.id}
                          held={false}
                        />
                      )}
                    </article>
                  ))
              )}
              {plan.hasNextPage && (
                <Button
                  variant="outline"
                  disabled={plan.isFetchingNextPage}
                  onClick={() => plan.fetchNextPage()}
                >
                  {t('admin:more')}
                </Button>
              )}
            </>
          ))}
      </section>
      <section className="settings-card">
        <h2>{t('admin:filesTitle')}</h2>
        {files.isPending ? (
          <StatePanel loading />
        ) : files.error ? (
          <StatePanel
            error={files.error}
            onRetry={files.refetch}
          />
        ) : (
          <>
            <p className="page-description">
              {t(files.data.deletionEnabled ? 'admin:deleteEnabled' : 'admin:deleteDisabled')}
            </p>
            {!files.data.items.length ? (
              <StatePanel title={t('admin:empty')} />
            ) : (
              files.data.items.map((item) => (
                <article
                  className="retention-plan-row"
                  key={item.id}
                >
                  <div>
                    <strong>{item.id}</strong>
                    <p>
                      {t(`admin:cleanupReasons.${item.reason}`, {
                        defaultValue: t('admin:cleanupReason'),
                      })}
                    </p>
                  </div>
                  <span>
                    {formatNumber(item.sizeBytes / 1024, { maximumFractionDigits: 1 })}{' '}
                    {t('files:kib')}
                  </span>
                </article>
              ))
            )}
            {files.data.hasMore && <p className="field-help">{t('admin:filesMore')}</p>}
          </>
        )}
      </section>
    </>
  )
}
