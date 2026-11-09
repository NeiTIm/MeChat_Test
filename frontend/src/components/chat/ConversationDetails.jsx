import { MessageCircle, ShieldCheck, Users } from 'lucide-react'
import { Avatar } from '@/components/Avatar'
import { StatePanel } from '@/components/StatePanel'
import { roleLabel, statusLabel } from '@/i18n'

export function ConversationDetails({
  t,
  selected,
  group,
  members,
  membersError,
  onRetryMembers,
  people,
  user,
  detailsExtra,
  preview,
}) {
  return (
    <div className="details-content">
      <div className="details-identity">
        <Avatar
          fileId={selected.avatarFileId}
          name={selected.name}
          tone={group ? 3 : 1}
        />
        <h3>{selected.name}</h3>
        <p>
          {group
            ? selected.description || t('chat:a-space-to-stay-in-the-loop')
            : selected.username
              ? `@${selected.username}`
              : t('chat:private-conversation')}
        </p>
      </div>
      <div className="detail-label">{t('chat:conversation')}</div>
      <div className="detail-row">
        <MessageCircle size={17} />
        <span>{group ? t('chat:group-conversation') : t('chat:direct-message')}</span>
      </div>
      {selected.status && (
        <div className="detail-row">
          <ShieldCheck size={17} />
          <span>
            {t('chat:status')} {statusLabel(selected.status)}
          </span>
        </div>
      )}
      {selected.myRole && (
        <div className="detail-row">
          <Users size={17} />
          <span>{t('chat:your-role')}</span>
          <span className="role-chip">{roleLabel(selected.myRole)}</span>
        </div>
      )}
      {group && (
        <>
          <div className="detail-label">
            {t('chat:members')}
            {members.length > 0 ? `· ${members.length}` : ''}
          </div>
          {membersError ? (
            <StatePanel
              compact
              error={membersError}
              onRetry={onRetryMembers}
            />
          ) : members.length === 0 ? (
            <p className="muted">{t('chat:member-information-is-loading-or-unavailable')}</p>
          ) : (
            <div className="members-list">
              {members.map((member, index) => (
                <div
                  className="member-row"
                  key={member.userId}
                >
                  <Avatar
                    small
                    name={people.get(member.userId)}
                    tone={index}
                  />
                  <span>
                    {people.get(member.userId)}
                    {member.userId === user?.id && <small> {t('chat:you')}</small>}
                  </span>
                  <span className="role-chip">{roleLabel(member.role)}</span>
                </div>
              ))}
            </div>
          )}
        </>
      )}
      {detailsExtra}
      <div className="details-note">
        <ShieldCheck size={18} />
        <p>
          {preview
            ? t('chat:sample-content-for-design-review-no-real-accounts-or-messages')
            : t(
                'chat:conversation-access-is-managed-by-mechat-availability-can-change-with-permissions',
              )}
        </p>
      </div>
    </div>
  )
}
