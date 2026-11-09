import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { Brand } from '@/components/Brand'
import { ThemeToggle } from '@/components/ThemeToggle'
import { Button } from '@/components/ui/button'
import { StatePanel } from '@/components/StatePanel'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { formatDate, formatTime } from '@/i18n/format'
import { statusLabel } from '@/i18n'
import { appealsApi } from './api'
import { queryClient } from '@/lib/query-client'

export function AppealsPage() {
  const { t } = useTranslation()
  const [access, setAccess] = useState(null)
  const [expired, setExpired] = useState(false)
  const [scope] = useState(() => crypto.randomUUID())
  const appeals = useQuery({
    queryKey: ['appeal', scope],
    queryFn: ({ signal }) => appealsApi.mine(access.accessToken, signal),
    enabled: !!access,
    gcTime: 0,
    retry: false,
  })
  function endSession() {
    queryClient.cancelQueries({ queryKey: ['appeal', scope] })
    queryClient.removeQueries({ queryKey: ['appeal', scope] })
    setAccess(null)
  }
  useEffect(() => {
    if (!access) return
    const timer = setTimeout(
      () => {
        setAccess(null)
        setExpired(true)
        queryClient.cancelQueries({ queryKey: ['appeal', scope] })
        queryClient.removeQueries({ queryKey: ['appeal', scope] })
      },
      Math.max(0, Date.parse(access.expiresAt) - Date.now()),
    )
    return () => clearTimeout(timer)
  }, [access, scope])
  useEffect(() => {
    if (appeals.error?.response?.status === 401) {
      setAccess(null)
      setExpired(true)
      queryClient.removeQueries({ queryKey: ['appeal', scope] })
    }
  }, [appeals.error, scope])
  useEffect(
    () => () => {
      queryClient.cancelQueries({ queryKey: ['appeal', scope] })
      queryClient.removeQueries({ queryKey: ['appeal', scope] })
    },
    [scope],
  )
  return (
    <main
      className="auth-flow-page appeals-page"
      id="main-content"
    >
      <header className="auth-flow-top">
        <Link
          to="/login"
          aria-label={t('auth:brandBackToLogin')}
        >
          <Brand />
        </Link>
        <ThemeToggle />
      </header>
      <section className="auth-flow-card">
        <span className="eyebrow">MECHAT</span>
        <h1>{t('appeals:title')}</h1>
        <p className="page-description">{t('appeals:description')}</p>
        {expired && (
          <p
            className="form-feedback"
            role="status"
          >
            {t('appeals:expired')}
          </p>
        )}
        {!access ? (
          <ValidatedForm
            id="appeal-login"
            fields={[
              { name: 'identifier', label: t('auth:fields.identifier'), autoComplete: 'username' },
              {
                name: 'password',
                label: t('auth:fields.password'),
                type: 'password',
                autoComplete: 'current-password',
              },
            ]}
            schema={z.object({
              identifier: z.string().trim().min(1, t('auth:requiredIdentifier')),
              password: z.string().min(1, t('auth:requiredPassword')),
            })}
            onSubmit={async (input) => {
              const session = await appealsApi.authenticate(input)
              if (!session.accessToken || !Number.isFinite(Date.parse(session.expiresAt)))
                throw new Error('INVALID_APPEAL_SESSION')
              setExpired(false)
              setAccess(session)
            }}
            submitLabel={t('appeals:authenticate')}
            resetOnSuccess
          />
        ) : (
          <>
            <p className="field-help">
              {t('appeals:expires', { time: formatTime(access.expiresAt) })}
            </p>
            <Button
              variant="outline"
              onClick={endSession}
            >
              {t('appeals:endSession')}
            </Button>
            <div className="section-divider" />
            {appeals.isPending ? (
              <StatePanel loading />
            ) : appeals.error ? (
              <StatePanel
                error={appeals.error}
                onRetry={appeals.refetch}
              />
            ) : (
              <>
                {appeals.data.some((appeal) => appeal.status === 'Pending') ? (
                  <p
                    className="form-feedback"
                    role="status"
                  >
                    {t('appeals:pending')}
                  </p>
                ) : (
                  <>
                    <h2>{t('appeals:reason')}</h2>
                    <p className="field-help">{t('appeals:reasonDescription')}</p>
                    <ValidatedForm
                      id="create-appeal"
                      fields={[
                        {
                          name: 'reason',
                          label: t('appeals:reason'),
                          type: 'textarea',
                          maxLength: 1000,
                        },
                      ]}
                      schema={z.object({
                        reason: z
                          .string()
                          .trim()
                          .min(10, t('appeals:reasonMin'))
                          .max(1000, t('appeals:reasonMax')),
                      })}
                      onSubmit={async ({ reason }) => {
                        await appealsApi.create(access.accessToken, reason)
                        await appeals.refetch()
                      }}
                      submitLabel={t('appeals:submit')}
                      successMessage={t('appeals:submitted')}
                      resetOnSuccess
                    />
                  </>
                )}
                <h2 className="appeal-history-heading">{t('appeals:history')}</h2>
                {!appeals.data.length ? (
                  <StatePanel
                    compact
                    title={t('appeals:empty')}
                  />
                ) : (
                  appeals.data.map((appeal) => (
                    <article
                      className="appeal-history-card"
                      key={appeal.id}
                    >
                      <span className="role-chip">{statusLabel(appeal.status)}</span>
                      <time>{formatDate(appeal.createdAt)}</time>
                      <p>{appeal.reason}</p>
                      {appeal.reviewNote && (
                        <>
                          <h3>{t('appeals:reviewNote')}</h3>
                          <p>{appeal.reviewNote}</p>
                        </>
                      )}
                    </article>
                  ))
                )}
              </>
            )}
          </>
        )}
        <nav className="auth-flow-links">
          <Link to="/login">{t('auth:backToLogin')}</Link>
        </nav>
      </section>
    </main>
  )
}
