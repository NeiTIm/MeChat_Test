import { useState } from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { usersApi } from '@/features/users/api'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { StatePanel } from '@/components/StatePanel'
import { useAuthStore } from '@/stores/auth-store'

export function UserPicker({ id, selected, onChange, multiple = false, exclude = [], disabled }) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  const [input, setInput] = useState('')
  const [query, setQuery] = useState('')
  const people = useInfiniteQuery({
    queryKey: ['workspace', 'userPicker', query],
    queryFn: ({ pageParam, signal }) => usersApi.search(query, pageParam, signal),
    initialPageParam: undefined,
    getNextPageParam: (page) => (page.hasMore ? page.nextCursor : undefined),
    enabled: !!query,
  })
  const items = [
    ...new Map(
      (people.data?.pages || []).flatMap((page) => page.items).map((item) => [item.id, item]),
    ).values(),
  ].filter((person) => person.id !== ownId && !exclude.includes(person.id))
  return (
    <div className="user-picker">
      <label htmlFor={id}>{t('friends:searchLabel')}</label>
      <div className="picker-search">
        <Input
          id={id}
          value={input}
          disabled={disabled}
          onChange={(event) => setInput(event.target.value)}
          maxLength={100}
          placeholder={t('friends:searchPlaceholder')}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault()
              setQuery(input.trim())
            }
          }}
        />
        <Button
          type="button"
          variant="outline"
          disabled={disabled || !input.trim()}
          onClick={() => setQuery(input.trim())}
        >
          {t('friends:search')}
        </Button>
      </div>
      {selected.length > 0 && (
        <div className="selected-people">
          {selected.map((person) => (
            <Button
              key={person.id}
              type="button"
              variant="secondary"
              disabled={disabled}
              onClick={() => onChange(selected.filter((row) => row.id !== person.id))}
              aria-label={t('groups:removeSelected', { name: person.displayName })}
            >
              {person.displayName}
              <span aria-hidden="true">×</span>
            </Button>
          ))}
        </div>
      )}
      {query &&
        (people.isPending ? (
          <StatePanel
            compact
            loading
          />
        ) : people.error ? (
          <StatePanel
            compact
            error={people.error}
            onRetry={people.refetch}
          />
        ) : items.length === 0 ? (
          <p className="field-help">{t('friends:noResults')}</p>
        ) : (
          <div className="picker-results">
            {items.map((person) => (
              <label key={person.id}>
                <input
                  type={multiple ? 'checkbox' : 'radio'}
                  name={`${id}-selected`}
                  disabled={disabled}
                  checked={selected.some((row) => row.id === person.id)}
                  onChange={(event) =>
                    onChange(
                      multiple
                        ? event.target.checked
                          ? [...selected, person]
                          : selected.filter((row) => row.id !== person.id)
                        : [person],
                    )
                  }
                />
                <span>
                  <strong>{person.displayName}</strong>
                  <small>@{person.username}</small>
                </span>
              </label>
            ))}
            {people.hasNextPage && (
              <Button
                type="button"
                variant="ghost"
                disabled={people.isFetchingNextPage}
                onClick={() => people.fetchNextPage()}
              >
                {people.isFetchingNextPage ? t('common:saving') : t('friends:more')}
              </Button>
            )}
          </div>
        ))}
    </div>
  )
}
