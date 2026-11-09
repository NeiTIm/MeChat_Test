import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { StatePanel } from '@/components/StatePanel'
import { ConfirmAction } from '@/components/ConfirmAction'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth-store'
import { formatDate, formatTime } from '@/i18n/format'
import { groupsApi } from '@/features/groups/api'

export function GroupLinks({ group, created, onCreated }) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  const [hours, setHours] = useState('24')
  const [copied, setCopied] = useState(false)
  const [copyError, setCopyError] = useState(false)
  const links = useQuery({
    queryKey: ['workspace', 'groupLinks', group.id],
    queryFn: ({ signal }) => groupsApi.links(group.id, signal),
  })
  const currentLink = created && links.data?.find((link) => link.id === created.id)
  const shareable =
    currentLink && (!currentLink.expiresAt || Date.parse(currentLink.expiresAt) > Date.now())
  return (
    <section
      className="settings-card"
      id="invite-links"
      tabIndex={-1}
    >
      <h2>{t('groups:linksTitle')}</h2>
      <p className="page-description">{t('groups:linksDescription')}</p>
      <ValidatedForm
        id="create-link"
        fields={[]}
        schema={z.object({})}
        submitLabel={t('groups:createLink')}
        onSubmit={async () => {
          const result = await groupsApi.createLink(
            group.id,
            hours === 'none' ? null : new Date(Date.now() + Number(hours) * 3600000).toISOString(),
          )
          if (typeof result?.token !== 'string' || !result.token.trim() || !result.id)
            throw new Error('INVALID_INVITE_RESPONSE')
          onCreated({
            id: result.id,
            url: `${window.location.origin}/groups/join#token=${encodeURIComponent(result.token)}`,
          })
          setCopied(false)
          await links.refetch()
        }}
      >
        <div className="form-field">
          <label htmlFor="link-expiry">{t('groups:expiry')}</label>
          <select
            id="link-expiry"
            value={hours}
            onChange={(event) => setHours(event.target.value)}
          >
            {['1', '24', '168', '720', 'none'].map((value) => (
              <option
                key={value}
                value={value}
              >
                {t(`groups:expiryOptions.${value}`)}
              </option>
            ))}
          </select>
        </div>
      </ValidatedForm>
      {created && shareable && (
        <div className="new-invite-link">
          <label htmlFor="created-invite-link">{t('groups:createdLink')}</label>
          <input
            id="created-invite-link"
            readOnly
            value={created.url}
            onFocus={(event) => event.target.select()}
          />
          <p className="field-help">{t('groups:linkOnce')}</p>
          <Button
            variant="outline"
            onClick={async () => {
              try {
                if (currentLink.expiresAt && Date.parse(currentLink.expiresAt) <= Date.now())
                  throw new Error('EXPIRED_LINK')
                await navigator.clipboard.writeText(created.url)
                setCopied(true)
                setCopyError(false)
              } catch {
                setCopyError(true)
              }
            }}
          >
            {t(copied ? 'groups:copied' : 'groups:copyLink')}
          </Button>
          {copied && (
            <p
              role="status"
              className="field-help"
            >
              {t('groups:copied')}
            </p>
          )}
          {copyError && (
            <p
              role="alert"
              className="field-error"
            >
              {t('groups:copyError')}
            </p>
          )}
        </div>
      )}
      {created && links.data && !links.isFetching && !shareable && (
        <p
          className="field-help"
          role="status"
        >
          {t('groups:linkUnavailable')}
        </p>
      )}
      {links.isPending ? (
        <StatePanel loading />
      ) : links.error ? (
        <StatePanel
          error={links.error}
          onRetry={links.refetch}
        />
      ) : !links.data.length ? (
        <StatePanel title={t('groups:noLinks')} />
      ) : (
        links.data.map((link) => (
          <article
            className="session-row"
            key={link.id}
          >
            <div>
              <h3>
                {t('groups:linkCreated', {
                  date: `${formatDate(link.createdAt)} · ${formatTime(link.createdAt)}`,
                })}
              </h3>
              <p>
                {link.expiresAt
                  ? t('groups:linkExpires', {
                      date: `${formatDate(link.expiresAt)} · ${formatTime(link.expiresAt)}`,
                    })
                  : t('groups:noExpiry')}
              </p>
              <span className="role-chip">
                {t(
                  link.expiresAt && Date.parse(link.expiresAt) <= Date.now()
                    ? 'groups:linkExpired'
                    : 'groups:linkActive',
                )}
              </span>
            </div>
            {(group.myRole === 'Owner' ||
              group.myRole === 'Admin' ||
              link.createdByUserId === ownId) && (
              <ConfirmAction
                label={t('groups:revokeLink')}
                description={t('groups:revokeDescription')}
                onConfirm={async () => {
                  await groupsApi.revokeLink(group.id, link.id)
                  if (created?.id === link.id) onCreated(null)
                  await links.refetch()
                }}
              />
            )}
          </article>
        ))
      )}
    </section>
  )
}
