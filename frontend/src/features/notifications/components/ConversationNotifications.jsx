import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'
import { queryClient } from '@/lib/query-client'
import { formatDate, formatTime } from '@/i18n/format'
import { notificationsApi } from '@/features/notifications/api'
import { showSuccess } from '@/stores/ui-feedback-store'

export function ConversationNotifications({ id }) {
  const { t } = useTranslation()
  const [hours, setHours] = useState('1')
  const settings = useQuery({
    queryKey: ['workspace', 'conversationSettings', id],
    queryFn: ({ signal }) => notificationsApi.settings(id, signal),
  })
  const action = useMutation({
    mutationFn: (muted) =>
      notificationsApi.mute(
        id,
        muted ? new Date(Date.now() + Number(hours) * 3600000).toISOString() : null,
      ),
    onSuccess: () => {
      showSuccess(t('notifications:saved'))
      return queryClient.invalidateQueries({ queryKey: ['workspace', 'conversationSettings', id] })
    },
  })
  return (
    <section className="conversation-notifications">
      <h3>{t('notifications:settingsTitle')}</h3>
      <p className="field-help">{t('notifications:settingsDescription')}</p>
      {settings.isPending ? (
        <StatePanel
          compact
          loading
        />
      ) : settings.error ? (
        <StatePanel
          compact
          error={settings.error}
          onRetry={settings.refetch}
        />
      ) : (
        <>
          <p className="field-help">
            {settings.data.mutedUntil && Date.parse(settings.data.mutedUntil) > Date.now()
              ? t('notifications:mutedUntil', {
                  date: `${formatDate(settings.data.mutedUntil)} · ${formatTime(settings.data.mutedUntil)}`,
                })
              : t('notifications:notMuted')}
          </p>
          <label htmlFor="mute-duration">{t('notifications:duration')}</label>
          <select
            id="mute-duration"
            value={hours}
            disabled={action.isPending}
            onChange={(event) => setHours(event.target.value)}
          >
            {['1', '8', '24', '168'].map((value) => (
              <option
                value={value}
                key={value}
              >
                {t(`notifications:durations.${value}`)}
              </option>
            ))}
          </select>
          <Button
            variant="outline"
            disabled={action.isPending}
            onClick={() => action.mutate(true)}
          >
            {t('notifications:mute')}
          </Button>
          {settings.data.mutedUntil && (
            <Button
              variant="ghost"
              disabled={action.isPending}
              onClick={() => action.mutate(false)}
            >
              {t('notifications:unmute')}
            </Button>
          )}
        </>
      )}
      {action.error && (
        <p
          role="alert"
          className="field-error"
        >
          {errorMessage(action.error)}
        </p>
      )}
      {action.isSuccess && (
        <p
          role="status"
          className="field-help"
        >
          {t('notifications:saved')}
        </p>
      )}
    </section>
  )
}
