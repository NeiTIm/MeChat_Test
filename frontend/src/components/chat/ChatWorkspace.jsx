import { useTranslation } from "react-i18next";
import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Link } from 'react-router';
import { ArrowDown, ArrowLeft, ArrowUpRight, Info, LoaderCircle, LogOut, MessageCircle, Pin, Search, ShieldCheck, Users, X } from 'lucide-react';
import { navigationItems } from '@/components/layout/navigation';
import { NavigationLinks } from '@/components/layout/NavigationLinks';
import { Brand } from '@/components/Brand';
import { ThemeToggle } from '@/components/ThemeToggle';
import { Avatar } from '@/components/Avatar';
import { StatePanel } from '@/components/StatePanel';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { errorMessage } from '@/lib/errors';
import { Composer } from './Composer';
import { TypingIndicator } from './TypingIndicator';
import { MessageEntry } from './MessageEntry';
import { useThreadScroll } from './use-thread-scroll';
import { useWideLayout } from '@/hooks/use-wide-layout';
import { roleLabel, statusLabel } from '@/i18n';
import { formatTime, formatDay } from '@/i18n/format';
import { useVisibleRead } from '@/features/messages/use-visible-read';
export function timeLabel(value) {
  return formatTime(value);
}
function dateLabel(value) {
  return formatDay(value);
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
  renderAttachment
}) {
  const {
    t
  } = useTranslation();
  const [filter, setFilter] = useState('');
  const [searchOpen, setSearchOpen] = useState(false);
  const [messageFilter, setMessageFilter] = useState('');
  const [detailsOpen, setDetailsOpen] = useState(false);
  const [online, setOnline] = useState(navigator.onLine);
  const detailsTrigger = useRef(null);
  const detailsHeading = useRef(null);
  const composerField = useRef(null);
  const [focusedTarget, setFocusedTarget] = useState(null);
  const [highlighted, setHighlighted] = useState(null);
  const wide = useWideLayout();
  const {
    scrollRef,
    onScroll,
    newMessages,
    jumpToLatest
  } = useThreadScroll(messages, selectedId, user?.id, !!focusMessageId);
  const title = titleOverride || (area === 'group' ? t("chat:groups") : t("chat:messages"));
  const navigation = navigationItems(t, { preview, area });
  useEffect(() => {
    setSearchOpen(false);
    setMessageFilter('');
    setDetailsOpen(false);
  }, [selectedId, focusMessageId, focusRequestKey]);
  useLayoutEffect(() => {
    if (!focusMessageId) { setFocusedTarget(null); setHighlighted(null); return }
    const key = `${selectedId}:${focusMessageId}:${focusRequestKey}`;
    if (focusedTarget === key) return;
    // Let panels close and filtered rows return before measuring the final viewport.
    if (detailsOpen || searchOpen || messageFilter) return;
    const target = [...(scrollRef.current?.querySelectorAll('[data-message-id]') || [])].find((node) => node.dataset.messageId === focusMessageId);
    if (!target) { setFocusedTarget(null); setHighlighted(null); return }
    setFocusedTarget(key);
    const root = scrollRef.current;
    root.scrollTop += target.getBoundingClientRect().top - root.getBoundingClientRect().top - (root.clientHeight - target.offsetHeight) / 2;
    target.focus({ preventScroll: true });
    setHighlighted(focusMessageId);
  }, [focusMessageId, focusRequestKey, focusedTarget, selectedId, messages, scrollRef, threadLoading, threadError, detailsOpen, searchOpen, messageFilter]);
  useEffect(() => {
    if (!highlighted) return;
    const timer = setTimeout(() => setHighlighted(null), 2500);
    return () => clearTimeout(timer);
  }, [highlighted, focusRequestKey]);
  useEffect(() => {
    const update = () => setOnline(navigator.onLine);
    window.addEventListener('online', update);
    window.addEventListener('offline', update);
    return () => {
      window.removeEventListener('online', update);
      window.removeEventListener('offline', update);
    };
  }, []);
  useEffect(() => {
    if (detailsOpen && wide) detailsHeading.current?.focus({
      preventScroll: true
    });
  }, [detailsOpen, wide]);
  function closeDetails() {
    setDetailsOpen(false);
    detailsTrigger.current?.focus({
      preventScroll: true
    });
  }
  async function sendMessage(input) {
    await onSend(input);
    requestAnimationFrame(() => composerField.current?.focus({
      preventScroll: true
    }));
  }
  const visibleConversations = conversations.filter(conversation => conversation.name.toLowerCase().includes(filter.toLowerCase()));
  const visibleMessages = messages.filter(message => !messageFilter || !message.recalled && message.content?.toLowerCase().includes(messageFilter.toLowerCase()));
  const people = new Map(members.map(member => [member.userId, member.displayName || member.username || roleLabel('Member')]));
  const jumpReady = !focusMessageId || focusedTarget === `${selectedId}:${focusMessageId}:${focusRequestKey}`;
  useVisibleRead({ scrollRef, messages: visibleMessages, selectedId, onRead: jumpReady ? onRead : undefined });
  people.set(user?.id, user?.name || t('common:you'));
  if (selected?.otherUserId) people.set(selected.otherUserId, selected.name);
  const group = area === 'group';
  const details = selected && <div className="details-content">
    <div className="details-identity"><Avatar fileId={selected.avatarFileId} name={selected.name} tone={group ? 3 : 1} /><h3>{selected.name}</h3><p>{group ? selected.description || t("chat:a-space-to-stay-in-the-loop") : selected.username ? `@${selected.username}` : t("chat:private-conversation")}</p></div>
    <div className="detail-label">{t("chat:conversation")}</div>
    <div className="detail-row"><MessageCircle size={17} /><span>{group ? t("chat:group-conversation") : t("chat:direct-message")}</span></div>
    {selected.status && <div className="detail-row"><ShieldCheck size={17} /><span>{t("chat:status")} {statusLabel(selected.status)}</span></div>}
    {selected.myRole && <div className="detail-row"><Users size={17} /><span>{t("chat:your-role")}</span><span className="role-chip">{roleLabel(selected.myRole)}</span></div>}
    {group && <><div className="detail-label">{t("chat:members")}{members.length > 0 ? `· ${members.length}` : ''}</div>
      {membersError ? <StatePanel compact error={membersError} onRetry={onRetryMembers} /> : members.length === 0 ? <p className="muted">{t("chat:member-information-is-loading-or-unavailable")}</p> : <div className="members-list">{members.map((member, index) => <div className="member-row" key={member.userId}><Avatar small name={people.get(member.userId)} tone={index} /><span>{people.get(member.userId)}{member.userId === user?.id && <small> {t("chat:you")}</small>}</span><span className="role-chip">{roleLabel(member.role)}</span></div>)}</div>}</>}
    {detailsExtra}
    <div className="details-note"><ShieldCheck size={18} /><p>{preview ? t("chat:sample-content-for-design-review-no-real-accounts-or-messages") : t("chat:conversation-access-is-managed-by-mechat-availability-can-change-with-permissions")}</p></div>
  </div>;
  return <div className={`workspace ${selectedId ? 'thread-selected' : ''} ${preview ? 'workspace-preview' : ''} ${wide && detailsOpen && selected && !threadError && !threadLoading ? 'details-open' : ''}`}>
    <a className="skip-link" href="#main-content">{t("chat:skip-to-conversation")}</a>
    <aside className="app-rail" aria-label={t("chat:application-sidebar")}>
      <Link className="rail-brand" to={navigation[0].to} aria-label={t("chat:mechat-messages")}><Brand compact /></Link>
      <nav className="rail-nav" aria-label={t("chat:main-navigation")}><NavigationLinks items={navigation} preview={preview} /></nav>
      <div className="rail-bottom"><ThemeToggle />{!preview && <Button variant="ghost" size="icon" className="icon-button" onClick={onLogout} disabled={logoutPending} aria-label={t("chat:sign-out")} title={t("chat:sign-out")}>{logoutPending ? <LoaderCircle className="spin" /> : <LogOut />}</Button>}<Avatar name={user?.name} small /></div>
    </aside>
    <section className="conversation-list" aria-label={title}>
      <header className="list-header"><div className="section-eyebrow">{t("chat:your-everyday-connections")}</div><div className="list-title"><h1>{title}<span className="brand-period">.</span></h1><span className="list-count">{conversations.length}</span><span className="mobile-theme"><ThemeToggle />{!preview && <Button variant="ghost" size="icon" className="icon-button" aria-label={t("chat:sign-out")} disabled={logoutPending} onClick={onLogout}><LogOut /></Button>}</span></div><p>{group ? t("chat:good-things-happen-together") : t("chat:a-little-hello-goes-a-long-way")}</p></header>
      <div className="list-search"><Search size={17} aria-hidden="true" /><Input aria-label={t("chat:filter-value0", {
          value0: title.toLowerCase()
        })} placeholder={t("chat:find-a-value0", {
          value0: group ? t('common:groups').toLowerCase() : t('common:conversation').toLowerCase()
        })} value={filter} onChange={event => setFilter(event.target.value)} />{filter && <Button variant="ghost" size="icon" aria-label={t("chat:clear-conversation-filter")} onClick={() => setFilter('')}><X size={14} /></Button>}</div>
      {listActions && <div className="workspace-list-actions">{listActions}</div>}
      <div className="list-section-label"><span>{filter ? t("chat:search-results") : group ? t("chat:your-groups") : t("chat:all-conversations")}</span><span>{visibleConversations.length}</span></div>
      <div className="conversation-scroll">{listLoading ? <StatePanel compact loading /> : listError ? <StatePanel compact error={listError} onRetry={onRetryList} /> : visibleConversations.length === 0 ? <StatePanel compact title={filter ? t("chat:no-matches") : t("chat:no-value0-yet", {
          value0: title.toLowerCase()
        })} description={filter ? t("chat:try-another-name") : t("chat:your-conversations-will-appear-here")} /> : visibleConversations.map((conversation, index) => <button className={`conversation-row ${selectedId === conversation.id ? 'selected' : ''}`} key={conversation.id} onClick={() => onSelect(conversation.id)} aria-current={selectedId === conversation.id ? 'true' : undefined}>
        <Avatar fileId={conversation.avatarFileId} name={conversation.name} tone={conversation.tone ?? index} />
        <span className="conversation-copy"><span className="conversation-name">{conversation.name}</span><span className="conversation-preview">{conversation.subtitle}</span></span>
        <span className="conversation-meta"><time>{conversation.time || timeLabel(conversation.updatedAt)}</time>{conversation.unread > 0 && <span className="unread-badge" aria-label={t("chat:value0-unread", {
              value0: conversation.unread
            })}>{conversation.unread}</span>}</span>
      </button>)}</div>
      <div className="list-footer"><span className={`connection-dot ${status === 'connected' ? 'connected' : ''}`} /><span>{!online ? t("chat:you-re-offline-waiting-for-connection") : preview ? t("chat:design-preview-sample-content") : status === 'connected' ? t("chat:connected-to-mechat") : status === 'connecting' || status === 'reconnecting' ? t("chat:connecting-to-mechat") : t("chat:realtime-disconnected")}</span></div>
      {logoutError && <p className="inline-error" role="alert">{errorMessage(logoutError)}</p>}
    </section>
    <main className="chat-main" id="main-content" tabIndex={-1}>
      {!online && <p className="offline-notice" role="status">{t("chat:you-re-offline-reconnect-then-try-sending-again")}</p>}
      {online && !['connected', 'disconnected'].includes(status) && <p className="connection-notice" role="status"><LoaderCircle className="spin" size={15} />{t("chat:reconnecting-your-conversation-will-sync-when-the-connection-returns")}{preview && <span>{t("chat:sample-state")}</span>}</p>}
      {online && !preview && status === 'disconnected' && <p className="connection-notice" role="status">{t("chat:realtime-is-disconnected-your-messages-may-be-delayed")}</p>}
      {!selectedId ? <div className="welcome-empty"><span className="empty-illustration"><MessageCircle size={46} /><span className="empty-spark"><Users size={20} /></span></span><span className="eyebrow">{t("chat:a-little-closer")}</span><h2>{t("chat:your-next-conversation")}<br />{t("chat:starts-with-a-hello")}<span className="brand-period">.</span></h2><p>{t(group ? 'chat:chooseGroup' : 'chat:choosePrivate')}</p><span className="empty-footnote">{preview ? t("chat:select-a-sample-conversation-to-explore-the-design") : t("chat:only-conversations-available-to-your-account-are-shown")}</span></div> : threadLoading ? <StatePanel loading /> : threadError ? <><Button variant="ghost" className="error-back" onClick={onBack}><ArrowLeft />{t("chat:back-to-value0", { value0: title.toLowerCase() })}</Button><StatePanel error={threadError} onRetry={onRetryThread} /></> : selected && <>
        <header className="thread-header">
          <Button variant="ghost" size="icon" className="back-button icon-button" onClick={onBack} aria-label={t("chat:back-to-value0", {
            value0: title.toLowerCase()
          })}><ArrowLeft /></Button>
          <Avatar fileId={selected.avatarFileId} name={selected.name} tone={group ? 3 : 1} small />
          <div className="thread-heading"><h1 title={selected.name}>{selected.name}</h1><p>{!group && typeof presenceStatus === 'boolean' && <span aria-hidden="true" className={`presence-dot ${presenceStatus ? 'presence-online' : 'presence-offline'}`} />}{headerSubtitle || (group ? membersLoading || membersError ? roleLabel(selected.myRole) : t('chat:memberSummary', {
                count: members.length,
                role: roleLabel(selected.myRole)
              }) : t("chat:private-conversation"))}</p></div>
          <div className="thread-actions">{headerTools}<Button variant="ghost" size="icon" className="icon-button" aria-label={t("chat:search-loaded-messages")} aria-expanded={searchOpen} onClick={() => {
              setSearchOpen(!searchOpen);
              setMessageFilter('');
            }}><Search /></Button>
            {wide ? <Button ref={detailsTrigger} variant="ghost" size="icon" className="icon-button" aria-label={t("chat:conversation-details")} aria-expanded={detailsOpen} onClick={() => setDetailsOpen(!detailsOpen)}><Info /></Button> : <Dialog open={detailsOpen} onOpenChange={setDetailsOpen}><DialogTrigger asChild><Button variant="ghost" size="icon" className="icon-button" aria-label={t("chat:conversation-details")}><Info /></Button></DialogTrigger><DialogContent className="details-dialog"><DialogHeader><DialogTitle>{t("chat:conversation-details")}</DialogTitle><DialogDescription>{group ? t("chat:about-this-group-and-its-members") : t("chat:about-this-conversation")}</DialogDescription></DialogHeader>{details}</DialogContent></Dialog>}
          </div>
        </header>
        {threadNotice}
        {searchOpen && <div className="message-search"><Search size={16} /><Input autoFocus value={messageFilter} onChange={event => setMessageFilter(event.target.value)} aria-label={t("chat:filter-loaded-messages")} placeholder={t("chat:search-loaded-messages")} /><span>{t("chat:resultCount", { count: visibleMessages.length })}</span></div>}
        {pinnedMessage && <button className="pinned-preview" onClick={onPinnedMessage}><Pin size={17} /><span><strong>{t("chat:pinned-message")}<small>{t("chat:sample")}</small></strong><span>{pinnedMessage.content}</span></span><ArrowUpRight size={16} /></button>}
        <div ref={scrollRef} onScroll={onScroll} className="message-scroll" aria-label={t("chat:message-history")} tabIndex={0}>
          {hasMore && <Button variant="ghost" className="load-more" onClick={onLoadMore} disabled={loadingMore}>{loadingMore ? <LoaderCircle className="spin" /> : <ArrowUpRight />}{t("chat:load-earlier-messages")}</Button>}
          {messages.length === 0 ? <StatePanel compact title={t("chat:a-fresh-start")} description={t("chat:be-the-first-to-say-hello")} /> : visibleMessages.length === 0 ? <StatePanel compact title={t("chat:no-matching-messages")} description={t("chat:try-another-phrase-in-the-loaded-history")} /> : visibleMessages.map((message, index) => {
            const own = message.senderUserId === user?.id;
            const showDate = index === 0 || dateLabel(message.createdAt) !== dateLabel(visibleMessages[index - 1].createdAt);
            const author = people.get(message.senderUserId) || roleLabel('Member');
            return <MessageEntry key={message.id} message={message} own={own} author={author} group={group} preview={preview} date={showDate ? dateLabel(message.createdAt) : null} highlighted={highlighted === message.id} focused={focusMessageId === message.id} renderMessageActions={renderMessageActions} renderMessageContext={renderMessageContext} renderMessageReactions={renderMessageReactions} renderMessageMeta={renderMessageMeta} renderAttachment={renderAttachment} />;
          })}
        </div>
        {newMessages && <Button className="jump-latest" variant="secondary" onClick={jumpToLatest}><ArrowDown size={16} />{t("chat:new-messages-jump-to-latest")}</Button>}
        <TypingIndicator names={[...new Set(typingActorIds)].filter(actorId => actorId !== user?.id).map(actorId => people.get(actorId) || t('groups:member'))} />
        <Composer key={`${selectedId}-${composerContext?.mode === 'edit' ? `edit-${composerContext.id}` : 'draft'}`} inputRef={composerField} onSend={sendMessage} disabled={composerDisabled || selected.status !== 'Active'} preview={preview} context={composerContext} onCancelContext={onCancelCompose} onTyping={onTyping} />
      </>}
    </main>
    {wide && detailsOpen && selected && !threadLoading && !threadError && <aside className="details-panel" aria-labelledby="details-panel-title" onKeyDown={event => {
      if (event.key === 'Escape' && event.currentTarget.contains(event.target)) closeDetails();
    }}><header><h2 ref={detailsHeading} id="details-panel-title" tabIndex={-1}>{t("chat:conversation-details")}</h2><Button variant="ghost" size="icon" className="icon-button" aria-label={t("chat:close-conversation-details")} onClick={closeDetails}><X /></Button></header><div className="details-panel-scroll">{details}</div></aside>}
    <nav className="mobile-nav" aria-label={t("chat:mobile-navigation")}><NavigationLinks items={navigation} mobile preview={preview} /></nav>
  </div>;
}
