import { Link, useNavigate } from 'react-router'
import { useMutation } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Button } from '@/components/ui/button'
import { roleLabel, statusLabel } from '@/i18n'
import { formatDate, formatTime, formatNumber } from '@/i18n/format'
import { errorMessage } from '@/lib/errors'
import { useAuthStore } from '@/stores/auth-store'
import { queryClient } from '@/lib/query-client'
import { AdminAction, HoldAction } from '@/features/admin/AdminActions'
import { adminApi } from '@/features/admin/api'
import { auditDetails } from '@/features/admin/format'

export function AdminRecord({ section, item }) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  const navigate = useNavigate()
  const claim = useMutation({
    mutationFn: () => adminApi.claim(item.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'admin'] })
      navigate(`/admin/reports/${item.id}`)
    },
  })
  const action = (method, label, options = {}) => (
    <AdminAction
      key={method}
      label={t(label)}
      target={item.username || item.name || item.id}
      onConfirm={(values) => adminApi[method](item.id, values)}
      {...options}
    />
  )
  return (
    <article className="admin-record">
      <div className="admin-record-content">
        {section === 'users' ? (
          <>
            <header>
              <h2>@{item.username}</h2>
              <span className="role-chip">{roleLabel(item.systemRole)}</span>
              <span className={`admin-status ${item.suspended ? 'suspended' : ''}`}>
                {t(item.suspended ? 'admin:suspended' : 'admin:active')}
              </span>
            </header>
            <p>{item.email}</p>
            <p className="field-help">
              {t('admin:created')}: {formatDate(item.createdAt)}
            </p>
          </>
        ) : section === 'groups' ? (
          <>
            <header>
              <h2>{item.name}</h2>
              <span className="role-chip">{statusLabel(item.status)}</span>
            </header>
            <p>{t('admin:memberCount', { count: formatNumber(item.memberCount) })}</p>
          </>
        ) : section === 'reports' ? (
          <>
            <header>
              <h2>{t(`reports:targets.${item.targetType}`)}</h2>
              <span className="role-chip">{statusLabel(item.status)}</span>
            </header>
            <p>{item.reason}</p>
            <p className="field-help">
              {formatDate(item.createdAt)} · <span className="record-id">{item.targetId}</span>
            </p>
          </>
        ) : section === 'appeals' ? (
          <>
            <header>
              <h2>{item.username || item.userId}</h2>
              <span className="role-chip">{statusLabel(item.status)}</span>
            </header>
            <p>{item.reason}</p>
            <p className="field-help">{formatDate(item.createdAt)}</p>
            {item.reviewNote && <p>{item.reviewNote}</p>}
          </>
        ) : (
          <>
            <header>
              <h2>
                {t(`admin:auditActions.${item.action}`, { defaultValue: t('admin:unknownAction') })}
              </h2>
              <span className="role-chip">
                {t(item.legalHold ? 'admin:held' : 'admin:notHeld')}
              </span>
            </header>
            <p className="field-help">
              {formatDate(item.createdAt)} · {formatTime(item.createdAt)}
            </p>
            <dl className="audit-details">
              <div>
                <dt>{t('admin:auditActor')}</dt>
                <dd>{item.adminUserId}</dd>
              </div>
              <div>
                <dt>{t('admin:auditTarget')}</dt>
                <dd>
                  {t(`admin:targetTypes.${item.targetType}`, {
                    defaultValue: t('admin:unknownTarget'),
                  })}{' '}
                  · {item.targetId}
                </dd>
              </div>
              {item.details && (
                <div>
                  <dt>{t('admin:auditDetails')}</dt>
                  <dd>{auditDetails(item)}</dd>
                </div>
              )}
            </dl>
          </>
        )}
      </div>
      <div className="admin-record-actions">
        {section === 'users' && item.id !== ownId && (
          <>
            {action('warn', 'admin:warn')}
            {item.systemRole !== 'Admin' &&
              (item.suspended
                ? action('unsuspend', 'admin:unsuspend')
                : action('suspend', 'admin:suspend', { destructive: true, suspension: true }))}
          </>
        )}
        {section === 'groups' &&
          item.status === 'Active' &&
          action('disableGroup', 'admin:disableGroup', { destructive: true })}
        {section === 'reports' && (
          <>
            {item.reviewedByAdminId === ownId ? (
              <Link
                className="text-link"
                to={`/admin/reports/${item.id}`}
              >
                {t('admin:viewCase')}
              </Link>
            ) : !item.reviewedByAdminId && item.status === 'Pending' ? (
              <Button
                variant="outline"
                disabled={claim.isPending}
                onClick={() => claim.mutate()}
              >
                {claim.isPending ? t('common:saving') : t('admin:claim')}
              </Button>
            ) : (
              <span className="field-help">{t('admin:assignedOther')}</span>
            )}
            {claim.error && (
              <p
                role="alert"
                className="field-error"
              >
                {errorMessage(claim.error)}
              </p>
            )}
          </>
        )}
        {section === 'appeals' && item.status === 'Pending' && (
          <>
            {['Accepted', 'Rejected'].map((status) => (
              <AdminAction
                key={status}
                field="note"
                label={t(status === 'Accepted' ? 'admin:acceptAppeal' : 'admin:rejectAppeal')}
                target={item.username || item.userId}
                description={t('admin:reviewAppealDescription')}
                onConfirm={({ note }) => adminApi.reviewAppeal(item.id, { status, note })}
              />
            ))}
          </>
        )}
        {section === 'audit' && (
          <HoldAction
            kind="Audit"
            id={item.id}
            held={item.legalHold}
          />
        )}
      </div>
    </article>
  )
}
