import { useQueries, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { StatePanel } from '@/components/StatePanel'
import { conversationsApi } from '@/features/conversations/api'
import { groupsApi } from '@/features/groups/api'
import { usersApi } from '@/features/users/api'

export function ForwardDestinations({ id, value, onChange, disabled }) {
  const { t } = useTranslation()
  const privateChats = useQuery({
    queryKey: ['workspace', 'private', 'list'],
    queryFn: conversationsApi.list,
  })
  const groups = useQuery({ queryKey: ['workspace', 'group', 'list'], queryFn: groupsApi.list })
  const ids = [...new Set((privateChats.data || []).map((chat) => chat.otherUserId))]
  const profiles = useQueries({
    queries: ids.map((userId) => ({
      queryKey: ['workspace', 'profile', userId],
      queryFn: ({ signal }) => usersApi.profile(userId, signal),
    })),
  })
  const names = new Map(
    ids.map((userId, index) => [
      userId,
      profiles[index].data?.displayName ||
        profiles[index].data?.username ||
        t('common:conversation'),
    ]),
  )
  if (privateChats.isPending || groups.isPending)
    return (
      <StatePanel
        compact
        loading
      />
    )
  if (privateChats.error || groups.error)
    return (
      <StatePanel
        compact
        error={privateChats.error || groups.error}
        onRetry={() => {
          privateChats.refetch()
          groups.refetch()
        }}
      />
    )
  return (
    <div className="form-field">
      <label htmlFor={id}>{t('messages:destination')}</label>
      <select
        id={id}
        value={value}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">{t('messages:chooseDestination')}</option>
        <optgroup label={t('common:messages')}>
          {(privateChats.data || [])
            .filter((chat) => chat.status === 'Active')
            .map((chat) => (
              <option
                value={chat.id}
                key={chat.id}
              >
                {names.get(chat.otherUserId)}
              </option>
            ))}
        </optgroup>
        <optgroup label={t('common:groups')}>
          {(groups.data || [])
            .filter((group) => group.status === 'Active')
            .map((group) => (
              <option
                value={group.id}
                key={group.id}
              >
                {group.name}
              </option>
            ))}
        </optgroup>
      </select>
    </div>
  )
}
