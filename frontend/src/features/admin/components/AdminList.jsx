import { useState } from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { formatNumber } from '@/i18n/format'
import { adminApi } from '@/features/admin/api'
import { AdminRecord } from './AdminRecord'

export function AdminList({ section }) {
  const { t } = useTranslation()
  const [filter, setFilter] = useState('')
  const records = useInfiniteQuery({
    queryKey: ['workspace', 'admin', section],
    queryFn: ({ pageParam, signal }) => adminApi.list(section, pageParam, signal),
    initialPageParam: 0,
    getNextPageParam: (last, pages) =>
      last.length === 100 ? pages.reduce((count, page) => count + page.length, 0) : undefined,
  })
  const items = [
    ...new Map((records.data?.pages || []).flat().map((item) => [item.id, item])).values(),
  ]
  const matching = items.filter((row) =>
    [row.username, row.email, row.name, row.reason, row.action, row.id]
      .filter(Boolean)
      .some((value) => value.toLowerCase().includes(filter.trim().toLowerCase())),
  )
  return (
    <section className="admin-records">
      <div className="admin-list-tools">
        <div>
          <label htmlFor="admin-filter">{t('admin:searchLoaded')}</label>
          <Input
            id="admin-filter"
            value={filter}
            onChange={(event) => setFilter(event.target.value)}
            placeholder={t('admin:searchPlaceholder')}
          />
        </div>
        <span>{t('admin:loaded', { count: formatNumber(items.length) })}</span>
      </div>
      {records.isPending ? (
        <StatePanel loading />
      ) : records.error ? (
        <StatePanel
          error={records.error}
          onRetry={records.refetch}
        />
      ) : !matching.length ? (
        <div className="settings-card">
          <StatePanel title={t('admin:empty')} />
        </div>
      ) : (
        <div className="admin-record-list">
          {matching.map((item) => (
            <AdminRecord
              key={item.id}
              section={section}
              item={item}
            />
          ))}
        </div>
      )}
      {records.hasNextPage && (
        <Button
          variant="outline"
          className="load-more-people"
          disabled={records.isFetchingNextPage}
          onClick={() => records.fetchNextPage()}
        >
          {records.isFetchingNextPage ? t('common:saving') : t('admin:more')}
        </Button>
      )}
    </section>
  )
}
