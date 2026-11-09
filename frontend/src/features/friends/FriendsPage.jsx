import { FriendRow } from '@/features/friends/components/FriendRow'
import { UserSearch } from '@/features/friends/components/UserSearch'
import { useState } from 'react'
import { NavLink, useNavigate, useParams } from 'react-router'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { UsersRound } from 'lucide-react'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { conversationsApi } from '@/features/conversations/api'
import { useAuthStore } from '@/stores/auth-store'
import { queryClient } from '@/lib/query-client'
import { errorMessage } from '@/lib/errors'
import { friendsApi } from './api'
import { useFriendUnread } from './use-friend-unread'
import { useRequestSeen } from './use-request-seen'

export function FriendsPage() {
  const { t } = useTranslation()
  const { section = 'list' } = useParams()
  const navigate = useNavigate()
  const userId = useAuthStore((state) => state.access?.userId)
  const unread = useFriendUnread()
  const seen = useRequestSeen(section === 'incoming' && unread.isSuccess)
  const [notice, setNotice] = useState('')
  const friends = useQuery({
    queryKey: ['workspace', 'friends'],
    queryFn: friendsApi.list,
  })
  const requests = useQuery({
    queryKey: ['workspace', 'friendRequests', userId],
    queryFn: friendsApi.requests,
  })
  const blocks = useQuery({
    queryKey: ['workspace', 'blocks'],
    queryFn: friendsApi.blocks,
  })
  const action = useMutation({
    mutationFn: async ({ method, id, success }) => {
      setNotice('')
      const result = await friendsApi[method](id)
      setNotice(
        t(
          method === 'send' && result?.status === 'Accepted'
            ? 'friends:accepted'
            : `friends:${success}`,
        ),
      )
      // Blocking changes visibility; discard protected content before fetching fresh data.
      if (method === 'block')
        await queryClient.resetQueries({
          queryKey: ['workspace'],
        })
      else
        await queryClient.invalidateQueries({
          queryKey: ['workspace'],
        })
    },
  })
  const openChat = useMutation({
    mutationFn: conversationsApi.open,
    onSuccess: (conversation) => {
      queryClient.invalidateQueries({
        queryKey: ['workspace'],
      })
      navigate(`/chat/${conversation.id}`)
    },
  })
  const run = (method, id, success) =>
    action.mutateAsync({
      method,
      id,
      success,
    })
  const shared = {
    friends: friends.data || [],
    requests: requests.data,
    blocks: blocks.data || [],
    registerSeen: seen.register,
    run,
    pending: action.isPending || openChat.isPending,
    onMessage: (id) => openChat.mutate(id),
  }
  const current = section === 'list' ? friends : section === 'blocked' ? blocks : requests
  const rows =
    section === 'incoming'
      ? requests.data?.incoming
      : section === 'outgoing'
        ? requests.data?.outgoing
        : section === 'blocked'
          ? blocks.data
          : friends.data
  return (
    <ApplicationLayout
      area="friends"
      title={t('friends:title')}
      description={t('friends:description')}
    >
      <div className="settings-layout">
        <nav
          className="settings-tabs"
          aria-label={t('friends:title')}
        >
          {['list', 'incoming', 'outgoing', 'search', 'blocked'].map((tab) => (
            <NavLink
              key={tab}
              to={`/friends/${tab}`}
              className={section === tab ? 'active' : ''}
            >
              {t(`friends:tabs.${tab}`)}
              {['incoming', 'outgoing'].includes(tab) && !!requests.data?.[tab]?.length && (
                <span className="tab-count">{requests.data[tab].length}</span>
              )}
            </NavLink>
          ))}
        </nav>
        <section className="settings-content">
          {notice && (
            <p
              className="form-feedback feedback-success"
              role="status"
            >
              {notice}
            </p>
          )}
          {seen.error && (
            <div
              className="form-feedback feedback-error"
              role="alert"
            >
              {errorMessage(seen.error)}{' '}
              <Button
                variant="outline"
                onClick={seen.retry}
              >
                {t('friends:retrySeen')}
              </Button>
            </div>
          )}
          {(action.error || openChat.error) && (
            <p
              className="form-feedback feedback-error"
              role="alert"
            >
              {errorMessage(action.error || openChat.error)}
            </p>
          )}
          {section === 'search' ? (
            <UserSearch
              {...shared}
              dependenciesError={friends.error || requests.error || blocks.error}
              dependenciesPending={friends.isPending || requests.isPending || blocks.isPending}
              onRetryDependencies={() => {
                friends.refetch()
                requests.refetch()
                blocks.refetch()
              }}
            />
          ) : !['list', 'incoming', 'outgoing', 'blocked'].includes(section) ? (
            <StatePanel title={t('common:nothing-here-just-yet')} />
          ) : current.isPending ? (
            <StatePanel loading />
          ) : current.error ? (
            <StatePanel
              error={current.error}
              onRetry={current.refetch}
            />
          ) : !rows?.length ? (
            <div className="people-empty">
              <UsersRound />
              <StatePanel
                title={t(
                  `friends:${section === 'incoming' ? 'noIncoming' : section === 'outgoing' ? 'noOutgoing' : section === 'blocked' ? 'noBlocked' : 'empty'}`,
                )}
                description={t('friends:emptyDescription')}
              />
            </div>
          ) : (
            <div className="people-list">
              {rows.map((row) => (
                <FriendRow
                  key={row.id || row.userId}
                  item={row}
                  section={section}
                  {...shared}
                />
              ))}
            </div>
          )}
        </section>
      </div>
    </ApplicationLayout>
  )
}
