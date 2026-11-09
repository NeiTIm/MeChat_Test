import { ConversationList } from './ConversationList'
import { ConversationDetails } from './ConversationDetails'
import { ThreadHeader } from './ThreadHeader'
import { useTranslation } from 'react-i18next'
import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import { Link } from 'react-router'
import {
  ArrowDown,
  ArrowLeft,
  ArrowUpRight,
  LoaderCircle,
  LogOut,
  MessageCircle,
  Pin,
  Search,
  Users,
  X,
} from 'lucide-react'
import { navigationItems } from '@/components/layout/navigation'
import { NavigationLinks } from '@/components/layout/NavigationLinks'
import { Brand } from '@/components/Brand'
import { ThemeToggle } from '@/components/ThemeToggle'
import { Avatar } from '@/components/Avatar'
import { StatePanel } from '@/components/StatePanel'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Composer } from './Composer'
import { TypingIndicator } from './TypingIndicator'
import { MessageEntry } from './MessageEntry'
import { useThreadScroll } from './use-thread-scroll'
import { useWideLayout } from '@/hooks/use-wide-layout'
import { roleLabel } from '@/i18n'
import { formatTime, formatDay } from '@/i18n/format'
import { useVisibleRead } from '@/features/messages/use-visible-read'

export function timeLabel(value) {
  return formatTime(value)
}
function dateLabel(value) {
  return formatDay(value)
}
export function ChatWorkspace({
  area = 'private',
  preview = false,
  user,
  conversations = [],
  selectedId,
  selected,
  messages = [],
  members = [],
  listLoading,
  threadLoading,
  listError,
  threadError,
  membersError,
  onSelect,
  onBack,
  onRetryList,
  onRetryThread,
  onRetryMembers,
  onSend,
  onLogout,
  logoutPending,
  logoutError,
  hasMore,
  onLoadMore,
  loadingMore,
  status = 'disconnected',
  renderMessageActions,
  detailsExtra,
  composerContext,
  onCancelCompose,
  pinnedMessage,
  onPinnedMessage,
  renderMessageMeta,
  renderMessageContext,
  renderMessageReactions,
  listActions,
  threadNotice,
  headerSubtitle,
  presenceStatus,
  headerTools,
  focusMessageId,
  focusRequestKey = '',
  onRead,
  onTyping,
  typingActorIds = [],
  composerDisabled,
  membersLoading,
  titleOverride,
  renderAttachment,
}) {
  const { t } = useTranslation()
  const [filter, setFilter] = useState('')
  const [searchOpen, setSearchOpen] = useState(false)
  const [messageFilter, setMessageFilter] = useState('')
  const [detailsOpen, setDetailsOpen] = useState(false)
  const [online, setOnline] = useState(navigator.onLine)
  const detailsTrigger = useRef(null)
  const detailsHeading = useRef(null)
  const composerField = useRef(null)
  const [focusedTarget, setFocusedTarget] = useState(null)
  const [highlighted, setHighlighted] = useState(null)
  const wide = useWideLayout()
  const { scrollRef, onScroll, newMessages, jumpToLatest } = useThreadScroll(
    messages,
    selectedId,
    user?.id,
    !!focusMessageId,
  )
  const title = titleOverride || (area === 'group' ? t('chat:groups') : t('chat:messages'))
  const navigation = navigationItems(t, { preview, area })
  useEffect(() => {
    setSearchOpen(false)
    setMessageFilter('')
    setDetailsOpen(false)
  }, [selectedId, focusMessageId, focusRequestKey])
  useLayoutEffect(() => {
    if (!focusMessageId) {
      setFocusedTarget(null)
      setHighlighted(null)
      return
    }
    const key = `${selectedId}:${focusMessageId}:${focusRequestKey}`
    if (focusedTarget === key) return
    // Let panels close and filtered rows return before measuring the final viewport.
    if (detailsOpen || searchOpen || messageFilter) return
    const target = [...(scrollRef.current?.querySelectorAll('[data-message-id]') || [])].find(
      (node) => node.dataset.messageId === focusMessageId,
    )
    if (!target) {
      setFocusedTarget(null)
      setHighlighted(null)
      return
    }
    setFocusedTarget(key)
    const root = scrollRef.current
    root.scrollTop +=
      target.getBoundingClientRect().top -
      root.getBoundingClientRect().top -
      (root.clientHeight - target.offsetHeight) / 2
    target.focus({ preventScroll: true })
    setHighlighted(focusMessageId)
  }, [
    focusMessageId,
    focusRequestKey,
    focusedTarget,
    selectedId,
    messages,
    scrollRef,
    threadLoading,
    threadError,
    detailsOpen,
    searchOpen,
    messageFilter,
  ])
  useEffect(() => {
    if (!highlighted) return
    const timer = setTimeout(() => setHighlighted(null), 2500)
    return () => clearTimeout(timer)
  }, [highlighted, focusRequestKey])
  useEffect(() => {
    const update = () => setOnline(navigator.onLine)
    window.addEventListener('online', update)
    window.addEventListener('offline', update)
    return () => {
      window.removeEventListener('online', update)
      window.removeEventListener('offline', update)
    }
  }, [])
  useEffect(() => {
    if (detailsOpen && wide)
      detailsHeading.current?.focus({
        preventScroll: true,
      })
  }, [detailsOpen, wide])
  function closeDetails() {
    setDetailsOpen(false)
    detailsTrigger.current?.focus({
      preventScroll: true,
    })
  }
  async function sendMessage(input) {
    await onSend(input)
    requestAnimationFrame(() =>
      composerField.current?.focus({
        preventScroll: true,
      }),
    )
  }
  const visibleConversations = conversations.filter((conversation) =>
    conversation.name.toLowerCase().includes(filter.toLowerCase()),
  )
  const visibleMessages = messages.filter(
    (message) =>
      !messageFilter ||
      (!message.recalled && message.content?.toLowerCase().includes(messageFilter.toLowerCase())),
  )
  const people = new Map(
    members.map((member) => [
      member.userId,
      member.displayName || member.username || roleLabel('Member'),
    ]),
  )
  const jumpReady =
    !focusMessageId || focusedTarget === `${selectedId}:${focusMessageId}:${focusRequestKey}`
  useVisibleRead({
    scrollRef,
    messages: visibleMessages,
    selectedId,
    onRead: jumpReady ? onRead : undefined,
  })
  people.set(user?.id, user?.name || t('common:you'))
  if (selected?.otherUserId) people.set(selected.otherUserId, selected.name)
  const group = area === 'group'
  const details = selected && (
    <ConversationDetails
      t={t}
      selected={selected}
      group={group}
      members={members}
      membersError={membersError}
      onRetryMembers={onRetryMembers}
      people={people}
      user={user}
      detailsExtra={detailsExtra}
      preview={preview}
    />
  )
  return (
    <div
      className={`workspace ${selectedId ? 'thread-selected' : ''} ${preview ? 'workspace-preview' : ''} ${wide && detailsOpen && selected && !threadError && !threadLoading ? 'details-open' : ''}`}
    >
      <a
        className="skip-link"
        href="#main-content"
      >
        {t('chat:skip-to-conversation')}
      </a>
      <aside
        className="app-rail"
        aria-label={t('chat:application-sidebar')}
      >
        <Link
          className="rail-brand"
          to={navigation[0].to}
          aria-label={t('chat:mechat-messages')}
        >
          <Brand compact />
        </Link>
        <nav
          className="rail-nav"
          aria-label={t('chat:main-navigation')}
        >
          <NavigationLinks
            items={navigation}
            preview={preview}
          />
        </nav>
        <div className="rail-bottom">
          <ThemeToggle />
          {!preview && (
            <Button
              variant="ghost"
              size="icon"
              className="icon-button"
              onClick={onLogout}
              disabled={logoutPending}
              aria-label={t('chat:sign-out')}
              title={t('chat:sign-out')}
            >
              {logoutPending ? <LoaderCircle className="spin" /> : <LogOut />}
            </Button>
          )}
          <Avatar
            name={user?.name}
            small
          />
        </div>
      </aside>
      <ConversationList
        t={t}
        title={title}
        conversations={conversations}
        group={group}
        preview={preview}
        logoutPending={logoutPending}
        onLogout={onLogout}
        filter={filter}
        setFilter={setFilter}
        listActions={listActions}
        visibleConversations={visibleConversations}
        listLoading={listLoading}
        listError={listError}
        onRetryList={onRetryList}
        selectedId={selectedId}
        onSelect={onSelect}
        status={status}
        online={online}
        logoutError={logoutError}
      />
      <main
        className="chat-main"
        id="main-content"
        tabIndex={-1}
      >
        {!online && (
          <p
            className="offline-notice"
            role="status"
          >
            {t('chat:you-re-offline-reconnect-then-try-sending-again')}
          </p>
        )}
        {online && !['connected', 'disconnected'].includes(status) && (
          <p
            className="connection-notice"
            role="status"
          >
            <LoaderCircle
              className="spin"
              size={15}
            />
            {t('chat:reconnecting-your-conversation-will-sync-when-the-connection-returns')}
            {preview && <span>{t('chat:sample-state')}</span>}
          </p>
        )}
        {online && !preview && status === 'disconnected' && (
          <p
            className="connection-notice"
            role="status"
          >
            {t('chat:realtime-is-disconnected-your-messages-may-be-delayed')}
          </p>
        )}
        {!selectedId ? (
          <div className="welcome-empty">
            <span className="empty-illustration">
              <MessageCircle size={46} />
              <span className="empty-spark">
                <Users size={20} />
              </span>
            </span>
            <span className="eyebrow">{t('chat:a-little-closer')}</span>
            <h2>
              {t('chat:your-next-conversation')}
              <br />
              {t('chat:starts-with-a-hello')}
              <span className="brand-period">.</span>
            </h2>
            <p>{t(group ? 'chat:chooseGroup' : 'chat:choosePrivate')}</p>
            <span className="empty-footnote">
              {preview
                ? t('chat:select-a-sample-conversation-to-explore-the-design')
                : t('chat:only-conversations-available-to-your-account-are-shown')}
            </span>
          </div>
        ) : threadLoading ? (
          <StatePanel loading />
        ) : threadError ? (
          <>
            <Button
              variant="ghost"
              className="error-back"
              onClick={onBack}
            >
              <ArrowLeft />
              {t('chat:back-to-value0', { value0: title.toLowerCase() })}
            </Button>
            <StatePanel
              error={threadError}
              onRetry={onRetryThread}
            />
          </>
        ) : (
          selected && (
            <>
              <ThreadHeader
                t={t}
                onBack={onBack}
                title={title}
                selected={selected}
                group={group}
                presenceStatus={presenceStatus}
                headerSubtitle={headerSubtitle}
                membersLoading={membersLoading}
                membersError={membersError}
                members={members}
                headerTools={headerTools}
                searchOpen={searchOpen}
                setSearchOpen={setSearchOpen}
                setMessageFilter={setMessageFilter}
                wide={wide}
                detailsTrigger={detailsTrigger}
                detailsOpen={detailsOpen}
                setDetailsOpen={setDetailsOpen}
                details={details}
              />
              {threadNotice}
              {searchOpen && (
                <div className="message-search">
                  <Search size={16} />
                  <Input
                    autoFocus
                    value={messageFilter}
                    onChange={(event) => setMessageFilter(event.target.value)}
                    aria-label={t('chat:filter-loaded-messages')}
                    placeholder={t('chat:search-loaded-messages')}
                  />
                  <span>{t('chat:resultCount', { count: visibleMessages.length })}</span>
                </div>
              )}
              {pinnedMessage && (
                <button
                  className="pinned-preview"
                  onClick={onPinnedMessage}
                >
                  <Pin size={17} />
                  <span>
                    <strong>
                      {t('chat:pinned-message')}
                      <small>{t('chat:sample')}</small>
                    </strong>
                    <span>{pinnedMessage.content}</span>
                  </span>
                  <ArrowUpRight size={16} />
                </button>
              )}
              <div
                ref={scrollRef}
                onScroll={onScroll}
                className="message-scroll"
                aria-label={t('chat:message-history')}
                tabIndex={0}
              >
                {hasMore && (
                  <Button
                    variant="ghost"
                    className="load-more"
                    onClick={onLoadMore}
                    disabled={loadingMore}
                  >
                    {loadingMore ? <LoaderCircle className="spin" /> : <ArrowUpRight />}
                    {t('chat:load-earlier-messages')}
                  </Button>
                )}
                {messages.length === 0 ? (
                  <StatePanel
                    compact
                    title={t('chat:a-fresh-start')}
                    description={t('chat:be-the-first-to-say-hello')}
                  />
                ) : visibleMessages.length === 0 ? (
                  <StatePanel
                    compact
                    title={t('chat:no-matching-messages')}
                    description={t('chat:try-another-phrase-in-the-loaded-history')}
                  />
                ) : (
                  visibleMessages.map((message, index) => {
                    const own = message.senderUserId === user?.id
                    const showDate =
                      index === 0 ||
                      dateLabel(message.createdAt) !==
                        dateLabel(visibleMessages[index - 1].createdAt)
                    const author = people.get(message.senderUserId) || roleLabel('Member')
                    return (
                      <MessageEntry
                        key={message.id}
                        message={message}
                        own={own}
                        author={author}
                        group={group}
                        preview={preview}
                        date={showDate ? dateLabel(message.createdAt) : null}
                        highlighted={highlighted === message.id}
                        focused={focusMessageId === message.id}
                        renderMessageActions={renderMessageActions}
                        renderMessageContext={renderMessageContext}
                        renderMessageReactions={renderMessageReactions}
                        renderMessageMeta={renderMessageMeta}
                        renderAttachment={renderAttachment}
                      />
                    )
                  })
                )}
              </div>
              {newMessages && (
                <Button
                  className="jump-latest"
                  variant="secondary"
                  onClick={jumpToLatest}
                >
                  <ArrowDown size={16} />
                  {t('chat:new-messages-jump-to-latest')}
                </Button>
              )}
              <TypingIndicator
                names={[...new Set(typingActorIds)]
                  .filter((actorId) => actorId !== user?.id)
                  .map((actorId) => people.get(actorId) || t('groups:member'))}
              />
              <Composer
                key={`${selectedId}-${composerContext?.mode === 'edit' ? `edit-${composerContext.id}` : 'draft'}`}
                inputRef={composerField}
                onSend={sendMessage}
                disabled={composerDisabled || selected.status !== 'Active'}
                preview={preview}
                context={composerContext}
                onCancelContext={onCancelCompose}
                onTyping={onTyping}
              />
            </>
          )
        )}
      </main>
      {wide && detailsOpen && selected && !threadLoading && !threadError && (
        <aside
          className="details-panel"
          aria-labelledby="details-panel-title"
          onKeyDown={(event) => {
            if (event.key === 'Escape' && event.currentTarget.contains(event.target)) closeDetails()
          }}
        >
          <header>
            <h2
              ref={detailsHeading}
              id="details-panel-title"
              tabIndex={-1}
            >
              {t('chat:conversation-details')}
            </h2>
            <Button
              variant="ghost"
              size="icon"
              className="icon-button"
              aria-label={t('chat:close-conversation-details')}
              onClick={closeDetails}
            >
              <X />
            </Button>
          </header>
          <div className="details-panel-scroll">{details}</div>
        </aside>
      )}
      <nav
        className="mobile-nav"
        aria-label={t('chat:mobile-navigation')}
      >
        <NavigationLinks
          items={navigation}
          mobile
          preview={preview}
        />
      </nav>
    </div>
  )
}
