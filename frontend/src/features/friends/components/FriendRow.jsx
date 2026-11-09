import { useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { MessageSquare } from 'lucide-react'
import { ConfirmAction } from '@/components/ConfirmAction'
import { Button } from '@/components/ui/button'
import { usersApi } from '@/features/users/api'
import { formatDate } from '@/i18n/format'
import { PublicProfile } from './PublicProfile'

export function FriendRow({ item, section, run, pending, onMessage, registerSeen }) {
  const { t } = useTranslation()
  const isRequest = section === 'incoming' || section === 'outgoing'
  const userId = isRequest
    ? section === 'incoming'
      ? item.senderUserId
      : item.receiverUserId
    : item.userId
  const profile = useQuery({
    queryKey: ['workspace', 'profile', userId],
    queryFn: ({ signal }) => usersApi.profile(userId, signal),
    enabled: isRequest,
  })
  const display = isRequest ? profile.data || {} : item
  const seenRef = useCallback(
    (node) => registerSeen?.(item.id, section === 'incoming' && item.seenAt == null ? node : null),
    [registerSeen, item.id, item.seenAt, section],
  )
  const perform = (method, success) =>
    run(method, isRequest ? item.id : userId, success).catch(() => {})
  return (
    <article
      ref={seenRef}
      className="person-row"
    >
      <div className="person-info">
        <PublicProfile
          id={userId}
          fallback={display}
        />
        <p className="person-date">
          {t(
            `friends:${isRequest ? 'requestDate' : section === 'blocked' ? 'blockedSince' : 'friendSince'}`,
            {
              date: formatDate(item.createdAt || item.blockedAt || item.since),
            },
          )}
        </p>
        {profile.error && <p className="field-help">{t('friends:profileUnavailable')}</p>}
      </div>
      <div className="person-actions">
        {section === 'incoming' ? (
          <>
            <Button
              disabled={pending}
              onClick={() => perform('accept', 'accepted')}
            >
              {t('friends:accept')}
            </Button>
            <Button
              variant="outline"
              disabled={pending}
              onClick={() => perform('reject', 'rejected')}
            >
              {t('friends:reject')}
            </Button>
          </>
        ) : section === 'outgoing' ? (
          <Button
            variant="outline"
            disabled={pending}
            onClick={() => perform('cancel', 'cancelled')}
          >
            {t('friends:cancel')}
          </Button>
        ) : section === 'blocked' ? (
          <ConfirmAction
            disabled={pending}
            label={t('friends:unblock')}
            description={t('friends:unblockConfirmation')}
            onConfirm={() => run('unblock', userId, 'unblocked')}
          />
        ) : (
          <>
            <Button
              disabled={pending}
              onClick={() => onMessage(userId)}
            >
              <MessageSquare size={16} />
              {t('friends:message')}
            </Button>
            <ConfirmAction
              disabled={pending}
              label={t('friends:unfriend')}
              description={t('friends:unfriendConfirmation')}
              onConfirm={() => run('unfriend', userId, 'unfriended')}
            />
          </>
        )}
        {section !== 'blocked' && (
          <ConfirmAction
            disabled={pending}
            destructive
            label={t('friends:block')}
            description={t('friends:blockConfirmation')}
            onConfirm={() => run('block', userId, 'blocked')}
          />
        )}
      </div>
    </article>
  )
}
