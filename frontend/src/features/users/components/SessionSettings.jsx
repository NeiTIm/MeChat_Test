import { useNavigate } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { MonitorSmartphone } from 'lucide-react'
import { StatePanel } from '@/components/StatePanel'
import { ConfirmAction } from '@/components/ConfirmAction'
import { authApi } from '@/features/auth/api'
import { broadcastLogout } from '@/app/providers/AuthProvider'
import { clearSession } from '@/lib/http'
import { formatDate, formatTime } from '@/i18n/format'
import { SettingsCard } from './SettingsCard'

export function SessionSettings() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const sessions = useQuery({ queryKey: ['workspace', 'sessions'], queryFn: authApi.sessions })
  if (sessions.isPending) return <StatePanel loading />
  if (sessions.error)
    return (
      <StatePanel
        error={sessions.error}
        onRetry={sessions.refetch}
      />
    )
  async function revoke(session) {
    await authApi.revokeSession(session.id)
    if (session.isCurrent) {
      clearSession()
      broadcastLogout()
      navigate('/login', { replace: true })
    } else await sessions.refetch()
  }
  return (
    <SettingsCard
      title={t('auth:sessionsTitle')}
      description={t('auth:sessionsDescription')}
    >
      <div className="section-actions">
        <ConfirmAction
          label={t('auth:logoutOthers')}
          description={t('auth:logoutOthersConfirmation')}
          onConfirm={async () => {
            await authApi.logoutOthers()
            await sessions.refetch()
          }}
        />
      </div>
      {sessions.data.length === 0 ? (
        <StatePanel
          compact
          title={t('auth:noSessions')}
        />
      ) : (
        <div className="session-list">
          {sessions.data.map((session) => (
            <article
              className="session-row"
              key={session.id}
            >
              <MonitorSmartphone />
              <div>
                <h3>
                  {session.deviceName || t('common:unknownDevice')}
                  {session.isCurrent && (
                    <span className="role-chip">{t('auth:currentSession')}</span>
                  )}
                </h3>
                <p>
                  {t('auth:sessionCreated', {
                    date: `${formatDate(session.createdAt)} · ${formatTime(session.createdAt)}`,
                  })}
                </p>
                <p>
                  {t('auth:sessionActive', {
                    date: `${formatDate(session.lastActivityAt)} · ${formatTime(session.lastActivityAt)}`,
                  })}
                </p>
              </div>
              <ConfirmAction
                label={session.isCurrent ? t('auth:signOut') : t('auth:revokeSession')}
                description={t('auth:revokeConfirmation')}
                onConfirm={() => revoke(session)}
              />
            </article>
          ))}
        </div>
      )}
    </SettingsCard>
  )
}
