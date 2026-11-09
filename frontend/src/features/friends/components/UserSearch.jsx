import { useState } from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Search, UserPlus, MessageSquare } from 'lucide-react'
import { StatePanel } from '@/components/StatePanel'
import { ConfirmAction } from '@/components/ConfirmAction'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { usersApi } from '@/features/users/api'
import { useAuthStore } from '@/stores/auth-store'
import { PublicProfile } from './PublicProfile'

export function UserSearch({
  friends,
  requests,
  blocks,
  run,
  pending,
  onMessage,
  dependenciesError,
  dependenciesPending,
  onRetryDependencies,
}) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  const [input, setInput] = useState('')
  const [query, setQuery] = useState('')
  const results = useInfiniteQuery({
    queryKey: ['workspace', 'userSearch', query],
    queryFn: ({ pageParam, signal }) => usersApi.search(query, pageParam, signal),
    initialPageParam: undefined,
    getNextPageParam: (page) => (page.hasMore ? page.nextCursor : undefined),
    enabled: !!query,
  })
  const items = [
    ...new Map(
      (results.data?.pages || []).flatMap((page) => page.items).map((item) => [item.id, item]),
    ).values(),
  ]
  return (
    <>
      <form
        className="people-search"
        onSubmit={(event) => {
          event.preventDefault()
          setQuery(input.trim())
        }}
      >
        <label htmlFor="people-query">{t('friends:searchLabel')}</label>
        <div>
          <Input
            id="people-query"
            value={input}
            onChange={(event) => setInput(event.target.value)}
            placeholder={t('friends:searchPlaceholder')}
            maxLength={100}
          />
          <Button
            type="submit"
            disabled={!input.trim()}
          >
            <Search size={18} />
            {t('friends:search')}
          </Button>
        </div>
      </form>
      {dependenciesPending ? (
        <StatePanel loading />
      ) : dependenciesError ? (
        <StatePanel
          error={dependenciesError}
          onRetry={onRetryDependencies}
        />
      ) : !query ? (
        <StatePanel title={t('friends:searchHint')} />
      ) : results.isPending ? (
        <StatePanel loading />
      ) : results.error ? (
        <StatePanel
          error={results.error}
          onRetry={results.refetch}
        />
      ) : (
        <>
          <p className="field-help">
            {t('friends:resultCount', {
              count: items.length,
            })}
          </p>
          {!items.length ? (
            <StatePanel
              title={t('friends:noResults')}
              description={t('friends:noResultsDescription')}
            />
          ) : (
            <div className="people-list">
              {items.map((person) => {
                const friend = friends.some((row) => row.userId === person.id)
                const blocked = blocks.some((row) => row.userId === person.id)
                const outgoing = requests?.outgoing?.some((row) => row.receiverUserId === person.id)
                const incoming = requests?.incoming?.find((row) => row.senderUserId === person.id)
                return (
                  <article
                    className="person-row"
                    key={person.id}
                  >
                    <PublicProfile
                      id={person.id}
                      fallback={person}
                    />
                    <div className="person-actions">
                      {person.id === ownId ? (
                        <span className="role-chip">{t('friends:yourAccount')}</span>
                      ) : blocked ? (
                        <ConfirmAction
                          disabled={pending}
                          label={t('friends:unblock')}
                          description={t('friends:unblockConfirmation')}
                          onConfirm={() => run('unblock', person.id, 'unblocked')}
                        />
                      ) : friend ? (
                        <>
                          <span className="role-chip">{t('friends:alreadyFriends')}</span>
                          <Button
                            disabled={pending}
                            onClick={() => onMessage(person.id)}
                          >
                            <MessageSquare size={16} />
                            {t('friends:message')}
                          </Button>
                        </>
                      ) : outgoing ? (
                        <span className="role-chip">{t('friends:pending')}</span>
                      ) : (
                        <Button
                          disabled={pending}
                          onClick={() =>
                            run(
                              incoming ? 'accept' : 'send',
                              incoming?.id || person.id,
                              incoming ? 'accepted' : 'sent',
                            ).catch(() => {})
                          }
                        >
                          <UserPlus size={16} />
                          {t(incoming ? 'friends:accept' : 'friends:send')}
                        </Button>
                      )}
                    </div>
                  </article>
                )
              })}
            </div>
          )}
          {results.hasNextPage && (
            <Button
              className="load-more-people"
              variant="outline"
              disabled={results.isFetchingNextPage}
              onClick={() => results.fetchNextPage()}
            >
              {results.isFetchingNextPage ? t('common:saving') : t('friends:more')}
            </Button>
          )}
        </>
      )}
    </>
  )
}
