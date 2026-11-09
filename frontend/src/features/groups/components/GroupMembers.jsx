import { useTranslation } from 'react-i18next'
import { ConfirmAction } from '@/components/ConfirmAction'
import { Avatar } from '@/components/Avatar'
import { queryClient } from '@/lib/query-client'
import { useAuthStore } from '@/stores/auth-store'
import { formatDate } from '@/i18n/format'
import { roleLabel } from '@/i18n'
import { groupsApi } from '@/features/groups/api'

export function GroupMembers({ group, members, refresh }) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  return (
    <div className="people-list">
      {members.map((member) => {
        const manageable =
          member.userId !== ownId &&
          member.role !== 'Owner' &&
          (group.myRole === 'Owner' || (group.myRole === 'Admin' && member.role === 'Member'))
        return (
          <article
            className="person-row"
            key={member.userId}
          >
            <div className="member-management-info">
              <Avatar name={member.displayName || member.username || '?'} />
              <div>
                <strong>{member.displayName || member.username || t('groups:member')}</strong>
                <p className="field-help">
                  {roleLabel(member.role)} ·{' '}
                  {t('groups:joined', { date: formatDate(member.joinedAt) })}
                </p>
              </div>
            </div>
            <div className="person-actions">
              {manageable && (
                <>
                  <ConfirmAction
                    label={t(member.role === 'Member' ? 'groups:promote' : 'groups:demote')}
                    description={t('groups:roleConfirmation')}
                    onConfirm={async () => {
                      await groupsApi.role(
                        group.id,
                        member.userId,
                        member.role === 'Member' ? 'Admin' : 'Member',
                      )
                      await refresh()
                    }}
                  />
                  <ConfirmAction
                    label={t('groups:removeMember')}
                    description={t('groups:removeConfirmation')}
                    destructive
                    onConfirm={async () => {
                      await groupsApi.remove(group.id, member.userId)
                      await queryClient.resetQueries({ queryKey: ['workspace'] })
                    }}
                  />
                </>
              )}
              {group.myRole === 'Owner' && member.userId !== ownId && (
                <ConfirmAction
                  label={t('groups:transfer')}
                  description={t('groups:transferConfirmation')}
                  onConfirm={async () => {
                    await groupsApi.transfer(group.id, member.userId)
                    await refresh()
                  }}
                />
              )}
            </div>
          </article>
        )
      })}
    </div>
  )
}
