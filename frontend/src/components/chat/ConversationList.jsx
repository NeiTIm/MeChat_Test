import { LogOut, Search, X } from 'lucide-react'
import { Avatar } from '@/components/Avatar'
import { ThemeToggle } from '@/components/ThemeToggle'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { errorMessage } from '@/lib/errors'
import { formatTime as timeLabel } from '@/i18n/format'

export function ConversationList({
  t,
  title,
  conversations,
  group,
  preview,
  logoutPending,
  onLogout,
  filter,
  setFilter,
  listActions,
  visibleConversations,
  listLoading,
  listError,
  onRetryList,
  selectedId,
  onSelect,
  status,
  online,
  logoutError,
}) {
  return (
    <section
      className="conversation-list"
      aria-label={title}
    >
      <header className="list-header">
        <div className="section-eyebrow">{t('chat:your-everyday-connections')}</div>
        <div className="list-title">
          <h1>
            {title}
            <span className="brand-period">.</span>
          </h1>
          <span className="list-count">{conversations.length}</span>
          <span className="mobile-theme">
            <ThemeToggle />
            {!preview && (
              <Button
                variant="ghost"
                size="icon"
                className="icon-button"
                aria-label={t('chat:sign-out')}
                disabled={logoutPending}
                onClick={onLogout}
              >
                <LogOut />
              </Button>
            )}
          </span>
        </div>
        <p>
          {group ? t('chat:good-things-happen-together') : t('chat:a-little-hello-goes-a-long-way')}
        </p>
      </header>
      <div className="list-search">
        <Search
          size={17}
          aria-hidden="true"
        />
        <Input
          aria-label={t('chat:filter-value0', {
            value0: title.toLowerCase(),
          })}
          placeholder={t('chat:find-a-value0', {
            value0: group
              ? t('common:groups').toLowerCase()
              : t('common:conversation').toLowerCase(),
          })}
          value={filter}
          onChange={(event) => setFilter(event.target.value)}
        />
        {filter && (
          <Button
            variant="ghost"
            size="icon"
            aria-label={t('chat:clear-conversation-filter')}
            onClick={() => setFilter('')}
          >
            <X size={14} />
          </Button>
        )}
      </div>
      {listActions && <div className="workspace-list-actions">{listActions}</div>}
      <div className="list-section-label">
        <span>
          {filter
            ? t('chat:search-results')
            : group
              ? t('chat:your-groups')
              : t('chat:all-conversations')}
        </span>
        <span>{visibleConversations.length}</span>
      </div>
      <div className="conversation-scroll">
        {listLoading ? (
          <StatePanel
            compact
            loading
          />
        ) : listError ? (
          <StatePanel
            compact
            error={listError}
            onRetry={onRetryList}
          />
        ) : visibleConversations.length === 0 ? (
          <StatePanel
            compact
            title={
              filter
                ? t('chat:no-matches')
                : t('chat:no-value0-yet', {
                    value0: title.toLowerCase(),
                  })
            }
            description={
              filter ? t('chat:try-another-name') : t('chat:your-conversations-will-appear-here')
            }
          />
        ) : (
          visibleConversations.map((conversation, index) => (
            <button
              className={`conversation-row ${selectedId === conversation.id ? 'selected' : ''}`}
              key={conversation.id}
              onClick={() => onSelect(conversation.id)}
              aria-current={selectedId === conversation.id ? 'true' : undefined}
            >
              <Avatar
                fileId={conversation.avatarFileId}
                name={conversation.name}
                tone={conversation.tone ?? index}
              />
              <span className="conversation-copy">
                <span className="conversation-name">{conversation.name}</span>
                <span className="conversation-preview">{conversation.subtitle}</span>
              </span>
              <span className="conversation-meta">
                <time>{conversation.time || timeLabel(conversation.updatedAt)}</time>
                {conversation.unread > 0 && (
                  <span
                    className="unread-badge"
                    aria-label={t('chat:value0-unread', {
                      value0: conversation.unread,
                    })}
                  >
                    {conversation.unread}
                  </span>
                )}
              </span>
            </button>
          ))
        )}
      </div>
      <div className="list-footer">
        <span className={`connection-dot ${status === 'connected' ? 'connected' : ''}`} />
        <span>
          {!online
            ? t('chat:you-re-offline-waiting-for-connection')
            : preview
              ? t('chat:design-preview-sample-content')
              : status === 'connected'
                ? t('chat:connected-to-mechat')
                : status === 'connecting' || status === 'reconnecting'
                  ? t('chat:connecting-to-mechat')
                  : t('chat:realtime-disconnected')}
        </span>
      </div>
      {logoutError && (
        <p
          className="inline-error"
          role="alert"
        >
          {errorMessage(logoutError)}
        </p>
      )}
    </section>
  )
}
