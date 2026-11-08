import { useTranslation } from "react-i18next";
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useInfiniteQuery, useMutation, useQueries, useQuery } from '@tanstack/react-query';
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router';
import { ChatWorkspace } from '@/components/chat/ChatWorkspace';
import { conversationsApi } from './api';
import { groupsApi } from '@/features/groups/api';
import { usersApi } from '@/features/users/api';
import { authApi } from '@/features/auth/api';
import { broadcastLogout } from '@/app/providers/AuthProvider';
import { useRealtime } from '@/app/providers/RealtimeProvider';
import { queryClient } from '@/lib/query-client';
import { useAuthStore } from '@/stores/auth-store';
import { roleLabel, statusLabel } from '@/i18n';
import { formatRelative } from '@/i18n/format';
import { messagesApi, requestsApi } from '@/features/messages/api';
import { MessageActions, MessageReply, MessageReactions, MessageReceipt } from '@/features/messages/MessageActions';
import { MessageSearch } from '@/features/messages/MessageSearch';
import { useMessageReceipts } from '@/features/messages/use-message-receipts';
import { StatePanel } from '@/components/StatePanel';
import { ConfirmAction } from '@/components/ConfirmAction';
import { Button } from '@/components/ui/button';
import { AttachmentPreview } from '@/features/files/AttachmentPreview';
import { ReportDialog } from '@/features/moderation/ReportsPage';
import { GroupPins } from '@/features/groups/GroupPins';
import { ConversationNotifications } from '@/features/notifications/NotificationsPage';
import { NotificationPreview } from '@/features/notifications/NotificationPreview';
import { showSuccess } from '@/stores/ui-feedback-store';
export function mergeMessages(pages = []) {
  const byId = new Map();
  for (const page of pages) for (const message of page.items) if (!byId.has(message.id)) byId.set(message.id, message);
  return [...byId.values()].sort((a, b) => a.sequence - b.sequence);
}
const renderLiveAttachment = (file) => <AttachmentPreview key={file.id} file={file} />;
const renderLiveContext = (message) => <MessageReply message={message} />;
const renderLiveReactions = (message) => <MessageReactions message={message} />;
const renderLiveReceipt = (message, own) => <MessageReceipt message={message} own={own} />;
export function WorkspacePage({
  area = 'private', requestMode = false
}) {
  const {
    t
  } = useTranslation();
  const {
    id
  } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const anchor = Number(searchParams.get('at'));
  const historyBefore = Number.isSafeInteger(anchor) && anchor > 0 && anchor < Number.MAX_SAFE_INTEGER ? anchor + 1 : undefined;
  const focusMessageId = historyBefore ? searchParams.get('message') : undefined;
  function jumpToMessage(message) {
    if (message.conversationId === id && Number.isSafeInteger(message.sequence) && message.sequence > 0 && message.sequence < Number.MAX_SAFE_INTEGER) {
      setSearchParams({ at: String(message.sequence), message: message.id });
    }
  }
  const access = useAuthStore(state => state.access);
  const {
    status, invoke, typing
  } = useRealtime();
  const api = area === 'group' ? groupsApi : conversationsApi;
  const base = area === 'group' ? '/groups' : requestMode ? '/message-requests' : '/chat';
  const [context, setContext] = useState(null);
  const typingLastSent = useRef(0);
  useEffect(() => { setContext(null) }, [id]);
  useEffect(() => {
    if (!id || status !== 'connected') return;
    invoke('JoinConversation', id);
    return () => { invoke('StopTyping', id); invoke('LeaveConversation', id) };
  }, [id, status, invoke]);
  const onTyping = useCallback((started) => {
    if (!id || requestMode) return;
    if (started && Date.now() - typingLastSent.current < 2500) return;
    typingLastSent.current = started ? Date.now() : 0;
    invoke(started ? 'StartTyping' : 'StopTyping', id);
  }, [id, requestMode, invoke]);
  const inbox = useQuery({ queryKey: ['workspace', 'messageRequests'], queryFn: requestsApi.list, enabled: area === 'private' });
  const requestCount = useQuery({ queryKey: ['workspace', 'requestCount'], queryFn: requestsApi.count, enabled: area === 'private' });
  const summary = useQuery({ queryKey: ['workspace', 'unreadSummary'], queryFn: messagesApi.summary });
  const list = useQuery({
    queryKey: ['workspace', area, 'list'],
    queryFn: api.list
  });
  const me = useQuery({
    queryKey: ['workspace', 'me'],
    queryFn: usersApi.me
  });
  const activeRequest = requestMode ? inbox.data?.find((request) => request.conversationId === id) : undefined;
  const source = requestMode ? (inbox.data || []).map((request) => ({ ...request, id: request.conversationId, otherUserId: request.senderUserId, updatedAt: request.createdAt })) : list.data || [];
  const detail = useQuery({
    queryKey: ['workspace', area, id, 'detail'],
    queryFn: ({
      signal
    }) => api.get(id, signal),
    enabled: !!id
  });
  const otherIds = [...new Set([...(area === 'private' ? source.map(conversation => conversation.otherUserId) : []), detail.data?.otherUserId].filter(Boolean))];
  const profileQueries = useQueries({
    queries: otherIds.map(otherId => ({
      queryKey: ['workspace', 'profile', otherId],
      queryFn: ({
        signal
      }) => usersApi.profile(otherId, signal)
    }))
  });
  const profiles = new Map(otherIds.map((otherId, index) => [otherId, profileQueries[index].data]));
  const members = useQuery({
    queryKey: ['workspace', 'group', id, 'members'],
    queryFn: ({
      signal
    }) => groupsApi.members(id, signal),
    enabled: area === 'group' && !!id
  });
  const history = useInfiniteQuery({
    queryKey: ['workspace', area, id, ...(historyBefore ? [historyBefore] : []), 'messages'],
    queryFn: ({
      pageParam,
      signal
    }) => api.history(id, pageParam, signal),
    initialPageParam: historyBefore,
    getNextPageParam: last => last.hasMore ? last.nextCursor : undefined,
    enabled: !!id
  });
  const send = useMutation({
    mutationFn: input => context?.mode === 'edit' ? messagesApi.edit(context.id, input.content) : api.send(id, input),
    onSuccess: () => { if (context?.mode === 'edit') showSuccess(t('messages:editSaved')); setContext(null); return queryClient.invalidateQueries({ queryKey: ['workspace'] }) }
  });
  const messageItems = useMemo(() => mergeMessages(history.data?.pages), [history.data]);
  const receipts = useMessageReceipts({ id, userId: access.userId, messages: messageItems, requestId: activeRequest?.id, enabled: !!id && !detail.error && !history.error && (!requestMode || !!activeRequest) });
  const threadUnread = useQuery({ queryKey: ['workspace', 'threadUnread', id], queryFn: ({ signal }) => messagesApi.unread(id, signal), enabled: !!id && !requestMode && !!history.data && !history.error });
  const presence = useQuery({ queryKey: ['workspace', 'presence', detail.data?.otherUserId], queryFn: ({ signal }) => messagesApi.presence(detail.data.otherUserId, signal), enabled: area === 'private' && !!detail.data?.otherUserId && !requestMode });
  const logout = useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      broadcastLogout();
      navigate('/login', {
        replace: true
      });
    }
  });
  const conversations = source.map(conversation => {
    const profile = profiles.get(conversation.otherUserId);
    return {
      ...conversation,
      avatarFileId: conversation.avatarFileId || profile?.avatarFileId,
      unread: summary.data?.conversations.find((entry) => entry.conversationId === conversation.id)?.count,
      name: area === 'group' ? conversation.name : profile?.displayName || profile?.username || t("common:conversation"),
      subtitle: requestMode ? t('messages:requestReadOnly') : area === 'group' ? conversation.description || t("common:your-role-value0", {
        value0: roleLabel(conversation.myRole)
      }) : t("common:private-conversation-value0", {
        value0: statusLabel(conversation.status)
      })
    };
  });
  const selectedProfile = profiles.get(detail.data?.otherUserId);
  const selected = detail.data && {
    ...detail.data,
    avatarFileId: detail.data.avatarFileId || selectedProfile?.avatarFileId,
    name: area === 'group' ? detail.data.name : selectedProfile?.displayName || selectedProfile?.username || t("common:conversation"),
    username: selectedProfile?.username
  };
  const activeGroupMember = area !== 'group' || ['Owner', 'Admin', 'Member'].includes(selected?.myRole);
  const renderActions = useCallback((message, own) => <MessageActions message={message} own={own} onCompose={setContext} canSend={!requestMode && activeGroupMember && selected?.status === 'Active'} onPin={area === 'group' && activeGroupMember && selected?.status === 'Active' ? (messageId) => groupsApi.pin(id, messageId) : undefined} />, [area, id, requestMode, selected?.status, activeGroupMember]);
  const requestActions = activeRequest && <RequestActions request={activeRequest} onDone={(accepted) => navigate(accepted ? `/chat/${id}` : '/message-requests')} />;
  const detailsExtra = <div className="live-thread-actions">{!requestMode && threadUnread.error && <StatePanel compact error={threadUnread.error} onRetry={threadUnread.refetch} />}{!requestMode && threadUnread.data && <p className="field-help">{t('messages:unreadDetail', { count: threadUnread.data.count })}</p>}{selected && <ReportDialog targetType={area === 'group' ? 'Group' : 'User'} targetId={area === 'group' ? id : selected.otherUserId} />}{requestMode ? requestActions : <>{id && <MessageSearch conversationId={id} onJump={jumpToMessage} />}{id && !requestMode && <ConversationNotifications id={id} />}{area === 'group' && <><Link className="text-link" to={`/groups/${id}/manage`}>{t('groups:manageTitle')}</Link><GroupPins groupId={id} canManage={activeGroupMember && selected?.status === 'Active'} /></>}<ConfirmAction label={t('messages:hideConversation')} description={t('messages:hideDescription')} onConfirm={async () => { await (area === 'group' ? groupsApi.hide(id) : conversationsApi.hide(id)); await queryClient.invalidateQueries({ queryKey: ['workspace'] }); navigate(base) }} />{area === 'private' && <ConfirmAction label={t('messages:renew')} description={t('messages:renewDescription')} onConfirm={async () => { await requestsApi.renew(id); await queryClient.invalidateQueries({ queryKey: ['workspace'] }) }} />}</>}</div>;
  const subtitle = requestMode ? t('messages:requestsTitle') : typing.some((item) => item.conversationId === id && item.actorId !== access.userId && item.expires > Date.now()) ? t('messages:typing') : presence.data?.online === true ? t('messages:online') : presence.data?.lastSeenAt ? t('messages:lastSeen', { time: formatRelative(presence.data.lastSeenAt) }) : presence.data?.online === false ? t('messages:offline') : undefined;
  return <ChatWorkspace area={area} titleOverride={requestMode ? t('messages:requestsTitle') : undefined} user={{
    id: access.userId,
    name: me.data?.displayName || access.username
  }} conversations={conversations} selectedId={id} selected={selected} members={members.data || []} membersLoading={members.isPending} messages={messageItems} listLoading={requestMode ? inbox.isPending : list.isPending} listError={requestMode ? inbox.error : list.error} threadLoading={!!id && (detail.isPending || history.isPending || requestMode && inbox.isPending)} threadError={detail.error || history.error} membersError={members.error} onSelect={selectedId => navigate(`${base}/${selectedId}`)} onBack={() => navigate(base)} onRetryList={requestMode ? inbox.refetch : list.refetch} onRetryThread={() => {
    detail.refetch();
    history.refetch();
  }} onRetryMembers={members.refetch} onSend={send.mutateAsync} hasMore={history.hasNextPage} loadingMore={history.isFetchingNextPage} onLoadMore={() => history.fetchNextPage()} status={status} onLogout={() => logout.mutate()} logoutPending={logout.isPending} logoutError={logout.error}
  listActions={area === 'private' ? <Link className="text-link" to={requestMode ? '/chat' : '/message-requests'}>{t(requestMode ? 'common:messages' : 'messages:requestsTitle')}{!requestMode && requestCount.data?.count > 0 && <span className="tab-count">{requestCount.data.count}</span>}</Link> : <>{[['/groups/new', 'groups:create'], ['/groups/invitations', 'groups:invitationsTitle'], ['/groups/join', 'groups:joinTitle']].map(([to, label]) => <Link className="text-link" key={to} to={to}>{t(label)}</Link>)}</>}
  detailsExtra={<>{area === 'group' && activeGroupMember && selected?.status === 'Active' && <GroupInviteCallout id={id} />}{detailsExtra}</>} headerSubtitle={subtitle} presenceStatus={requestMode ? undefined : presence.data?.online} headerTools={<NotificationPreview />} focusMessageId={focusMessageId} onRead={receipts.onRead} onTyping={onTyping} composerDisabled={requestMode || !activeGroupMember} composerContext={context} onCancelCompose={() => setContext(null)}
  threadNotice={<>{historyBefore && <div className="context-history-notice" role="status"><p>{history.data && focusMessageId && !messageItems.some((message) => message.id === focusMessageId) ? t('messages:contextUnavailable') : t('messages:contextHistory')}</p><Button variant="link" onClick={() => setSearchParams({})}>{t('messages:backToLatest')}</Button></div>}{area === 'group' && location.state?.createdGroupId === id && <div className="group-created-notice"><p>{t('groups:createdHint')}</p><Link className="text-link" to={`/groups/${id}/manage#invite-links`}>{t('groups:inviteCta')}</Link></div>}{requestMode && <div className="request-notice"><p>{t('messages:requestReadOnly')}</p>{requestActions}</div>}{receipts.error && <div className="connection-notice" role="status">{t('messages:receiptError')}<Button variant="link" onClick={receipts.retry}>{t('messages:retryReceipts')}</Button></div>}</>}
  renderMessageActions={renderActions} renderAttachment={renderLiveAttachment}
  renderMessageContext={renderLiveContext} renderMessageReactions={renderLiveReactions} renderMessageMeta={renderLiveReceipt} />;
}
function GroupInviteCallout({ id }) {
  const { t } = useTranslation();
  return <section className="group-invite-cta"><Link className="text-link" to={`/groups/${id}/manage#invite-links`}>{t('groups:inviteCta')}</Link><p className="field-help">{t('groups:inviteHint')}</p></section>;
}
function RequestActions({ request, onDone }) {
  const { t } = useTranslation();
  const respond = async (method) => { await requestsApi[method](request.id); await queryClient.resetQueries({ queryKey: ['workspace'] }); onDone(method === 'accept') };
  return <div className="request-actions">{['accept', 'reject', 'block'].map((method) => <ConfirmAction key={method} label={t(`messages:request${method[0].toUpperCase()}${method.slice(1)}`)} description={t(`messages:request${method[0].toUpperCase()}${method.slice(1)}Description`)} destructive={method === 'block'} onConfirm={() => respond(method)} />)}</div>;
}
