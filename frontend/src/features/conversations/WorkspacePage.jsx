import { mergeMessages } from '@/features/conversations/utils/merge-messages'
import { RequestActions } from '@/features/conversations/components/RequestActions'
import { useConversationTyping } from './hooks/use-conversation-typing'
import { useConversationHistory } from './hooks/use-conversation-history'
import { useTranslation } from 'react-i18next'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { useMutation, useQueries, useQuery } from '@tanstack/react-query'
import { Link, useLocation, useNavigate, useParams, useSearchParams } from 'react-router'
import { ChatWorkspace } from '@/components/chat/ChatWorkspace'
import { conversationsApi } from './api'
import { groupsApi } from '@/features/groups/api'
import { usersApi } from '@/features/users/api'
import { authApi } from '@/features/auth/api'
import { broadcastLogout } from '@/app/providers/AuthProvider'
import { useRealtime } from '@/app/providers/RealtimeProvider'
import { queryClient } from '@/lib/query-client'
import { useAuthStore } from '@/stores/auth-store'
import { roleLabel, statusLabel } from '@/i18n'
import { formatRelative } from '@/i18n/format'
import { messagesApi, requestsApi } from '@/features/messages/api'
import { MessageActions } from '@/features/messages/MessageActions'
import {
  MessageReply,
  MessageReactions,
  MessageReceipt,
} from '@/features/messages/components/MessageMetadata'
import { MessageSearch } from '@/features/messages/MessageSearch'
import { useMessageReceipts } from '@/features/messages/use-message-receipts'
import { StatePanel } from '@/components/StatePanel'
import { ConfirmAction } from '@/components/ConfirmAction'
import { Button } from '@/components/ui/button'
import { AttachmentPreview } from '@/features/files/AttachmentPreview'
import { ReportDialog } from '@/features/moderation/components/ReportDialog'
import { GroupPins } from '@/features/groups/GroupPins'
import { ConversationNotifications } from '@/features/notifications/components/ConversationNotifications'
import { NotificationPreview } from '@/features/notifications/NotificationPreview'
import { showSuccess } from '@/stores/ui-feedback-store'
import { refreshMessageContext, refreshMessageThread } from '@/features/messages/message-events'

export { mergeMessages } from '@/features/conversations/utils/merge-messages'

const renderLiveAttachment = (file) => (
  <AttachmentPreview
    key={file.id}
    file={file}
  />
)
const renderLiveContext = (message) => <MessageReply message={message} />
const renderLiveReactions = (message) => <MessageReactions message={message} />
const renderLiveReceipt = (message, own) => (
  <MessageReceipt
    message={message}
    own={own}
  />
)
export function WorkspacePage({ area = 'private', requestMode = false }) {
  const { t } = useTranslation()
  const { id } = useParams()
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams, setSearchParams] = useSearchParams()
  const anchor = Number(searchParams.get('at'))
  const anchoredBefore =
    Number.isSafeInteger(anchor) && anchor > 0 && anchor < Number.MAX_SAFE_INTEGER
      ? anchor + 1
      : undefined
  const focusMessageId = anchoredBefore ? searchParams.get('message') : undefined
  // Reuse an already-loaded page; after reload, go directly to the bounded cursor.
  const cachedTarget = useMemo(
    () =>
      !!focusMessageId &&
      (queryClient.getQueryData(['workspace', area, id, 'messages'])?.pages || []).some((page) =>
        page.items.some((message) => message.id === focusMessageId),
      ),
    [area, id, focusMessageId],
  )
  const historyBefore = cachedTarget ? undefined : anchoredBefore
  function jumpToMessage(message) {
    if (
      message.conversationId === id &&
      Number.isSafeInteger(message.sequence) &&
      message.sequence > 0 &&
      message.sequence < Number.MAX_SAFE_INTEGER
    ) {
      setSearchParams({ at: String(message.sequence), message: message.id })
    }
  }
  const access = useAuthStore((state) => state.access)
  const { status, invoke, typing } = useRealtime()
  const isGroup = area === 'group'
  const api = isGroup ? groupsApi : conversationsApi
  const privateBasePath = requestMode ? '/message-requests' : '/chat'
  const conversationBasePath = isGroup ? '/groups' : privateBasePath
  const [context, setContext] = useState(null)
  useEffect(() => {
    setContext(null)
  }, [id])
  const onTyping = useConversationTyping({ id, status, invoke, requestMode })
  const inbox = useQuery({
    queryKey: ['workspace', 'messageRequests'],
    queryFn: requestsApi.list,
    enabled: area === 'private',
  })
  const requestCount = useQuery({
    queryKey: ['workspace', 'requestCount'],
    queryFn: requestsApi.count,
    enabled: area === 'private',
  })
  const summary = useQuery({
    queryKey: ['workspace', 'unreadSummary'],
    queryFn: messagesApi.summary,
  })
  const list = useQuery({
    queryKey: ['workspace', area, 'list'],
    queryFn: api.list,
  })
  const conversationListQuery = requestMode ? inbox : list
  const me = useQuery({
    queryKey: ['workspace', 'me'],
    queryFn: usersApi.me,
  })
  const activeRequest = requestMode
    ? inbox.data?.find((request) => request.conversationId === id)
    : undefined
  const conversationSources = requestMode
    ? (inbox.data || []).map((request) => ({
        ...request,
        id: request.conversationId,
        otherUserId: request.senderUserId,
        updatedAt: request.createdAt,
      }))
    : list.data || []
  const detail = useQuery({
    queryKey: ['workspace', area, id, 'detail'],
    queryFn: ({ signal }) => api.get(id, signal),
    enabled: !!id,
  })
  const peerUserIds = [
    ...new Set(
      [
        ...(area === 'private'
          ? conversationSources.map((conversation) => conversation.otherUserId)
          : []),
        detail.data?.otherUserId,
      ].filter(Boolean),
    ),
  ]
  const profileQueries = useQueries({
    queries: peerUserIds.map((peerUserId) => ({
      queryKey: ['workspace', 'profile', peerUserId],
      queryFn: ({ signal }) => usersApi.profile(peerUserId, signal),
    })),
  })
  const profilesByUserId = new Map(
    peerUserIds.map((peerUserId, index) => [peerUserId, profileQueries[index].data]),
  )
  const members = useQuery({
    queryKey: ['workspace', 'group', id, 'members'],
    queryFn: ({ signal }) => groupsApi.members(id, signal),
    enabled: area === 'group' && !!id,
  })
  const history = useConversationHistory({ area, id, historyBefore, cachedTarget, api })
  const send = useMutation({
    mutationFn: (input) =>
      context?.mode === 'edit' ? messagesApi.edit(context.id, input.content) : api.send(id, input),
    onSuccess: () => {
      if (context?.mode === 'edit') {
        showSuccess(t('messages:editSaved'))
        refreshMessageContext(queryClient, context.id, access.userId, true)
        queryClient.invalidateQueries({ queryKey: ['workspace', 'message', context.id] })
      }
      setContext(null)
      return refreshMessageThread(queryClient, id)
    },
  })
  const messageItems = useMemo(() => mergeMessages(history.data?.pages), [history.data])
  const receipts = useMessageReceipts({
    id,
    userId: access.userId,
    messages: messageItems,
    requestId: activeRequest?.id,
    enabled: !!id && !detail.error && !history.error && (!requestMode || !!activeRequest),
  })
  const threadUnread = useQuery({
    queryKey: ['workspace', 'threadUnread', id],
    queryFn: ({ signal }) => messagesApi.unread(id, signal),
    enabled: !!id && !requestMode && !!history.data && !history.error,
  })
  const presence = useQuery({
    queryKey: ['workspace', 'presence', detail.data?.otherUserId],
    queryFn: ({ signal }) => messagesApi.presence(detail.data.otherUserId, signal),
    enabled: area === 'private' && !!detail.data?.otherUserId && !requestMode,
  })
  const logout = useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      broadcastLogout()
      navigate('/login', {
        replace: true,
      })
    },
  })
  function getConversationName(conversation, profile) {
    if (isGroup) return conversation.name
    return profile?.displayName || profile?.username || t('common:conversation')
  }

  function getConversationSubtitle(conversation) {
    if (requestMode) return t('messages:requestReadOnly')
    if (isGroup) {
      return (
        conversation.description ||
        t('common:your-role-value0', { value0: roleLabel(conversation.myRole) })
      )
    }
    return t('common:private-conversation-value0', { value0: statusLabel(conversation.status) })
  }

  const conversations = conversationSources.map((conversation) => {
    const profile = profilesByUserId.get(conversation.otherUserId)
    return {
      ...conversation,
      avatarFileId: conversation.avatarFileId || profile?.avatarFileId,
      unread: summary.data?.conversations.find((entry) => entry.conversationId === conversation.id)
        ?.count,
      name: getConversationName(conversation, profile),
      subtitle: getConversationSubtitle(conversation),
    }
  })
  const selectedProfile = profilesByUserId.get(detail.data?.otherUserId)
  const selected = detail.data && {
    ...detail.data,
    avatarFileId: detail.data.avatarFileId || selectedProfile?.avatarFileId,
    name: getConversationName(detail.data, selectedProfile),
    username: selectedProfile?.username,
  }
  const activeGroupMember =
    area !== 'group' || ['Owner', 'Admin', 'Member'].includes(selected?.myRole)
  const canManageGroup = isGroup && activeGroupMember && selected?.status === 'Active'
  const renderActions = useCallback(
    (message, own) => (
      <MessageActions
        message={message}
        own={own}
        onCompose={setContext}
        canSend={!requestMode && activeGroupMember && selected?.status === 'Active'}
        onPin={
          area === 'group' && activeGroupMember && selected?.status === 'Active'
            ? (messageId) => groupsApi.pin(id, messageId)
            : undefined
        }
      />
    ),
    [area, id, requestMode, selected?.status, activeGroupMember],
  )
  const requestActions = activeRequest && (
    <RequestActions
      request={activeRequest}
      onDone={(accepted) => navigate(accepted ? `/chat/${id}` : '/message-requests')}
    />
  )

  async function hideConversation() {
    await api.hide(id)
    await queryClient.invalidateQueries({ queryKey: ['workspace'] })
    navigate(conversationBasePath)
  }

  async function renewMessageRequest() {
    await requestsApi.renew(id)
    await queryClient.invalidateQueries({ queryKey: ['workspace'] })
  }

  function retryThread() {
    detail.refetch()
    history.refetch()
  }

  const detailsExtra = (
    <div className="live-thread-actions">
      {!requestMode && threadUnread.error && (
        <StatePanel
          compact
          error={threadUnread.error}
          onRetry={threadUnread.refetch}
        />
      )}
      {!requestMode && threadUnread.data && (
        <p className="field-help">
          {t('messages:unreadDetail', { count: threadUnread.data.count })}
        </p>
      )}
      {selected && (
        <ReportDialog
          targetType={area === 'group' ? 'Group' : 'User'}
          targetId={area === 'group' ? id : selected.otherUserId}
        />
      )}
      {requestMode ? (
        requestActions
      ) : (
        <>
          {id && (
            <MessageSearch
              conversationId={id}
              onJump={jumpToMessage}
            />
          )}
          {id && !requestMode && <ConversationNotifications id={id} />}
          {area === 'group' && (
            <>
              <Link
                className="text-link"
                to={`/groups/${id}/manage`}
              >
                {t('groups:manageTitle')}
              </Link>
              <GroupPins
                groupId={id}
                canManage={canManageGroup}
              />
            </>
          )}
          <ConfirmAction
            label={t('messages:hideConversation')}
            description={t('messages:hideDescription')}
            onConfirm={hideConversation}
          />
          {area === 'private' && (
            <ConfirmAction
              label={t('messages:renew')}
              description={t('messages:renewDescription')}
              onConfirm={renewMessageRequest}
            />
          )}
        </>
      )}
    </div>
  )
  const typingActorIds = requestMode
    ? []
    : typing
        .filter(
          (item) =>
            item.conversationId === id &&
            item.actorId !== access.userId &&
            item.expires > Date.now(),
        )
        .map((item) => item.actorId)
  function getHeaderSubtitle() {
    if (requestMode) return t('messages:requestsTitle')
    if (presence.data?.online === true) return t('messages:online')
    if (presence.data?.lastSeenAt) {
      return t('messages:lastSeen', { time: formatRelative(presence.data.lastSeenAt) })
    }
    if (presence.data?.online === false) return t('messages:offline')
    return undefined
  }

  const headerSubtitle = getHeaderSubtitle()
  const threadLoading =
    !!id && (detail.isPending || history.isPending || (requestMode && inbox.isPending))
  const targetUnavailable =
    history.data && focusMessageId && !messageItems.some((message) => message.id === focusMessageId)
  const listActions =
    area === 'private' ? (
      <Link
        className="text-link"
        to={requestMode ? '/chat' : '/message-requests'}
      >
        {t(requestMode ? 'common:messages' : 'messages:requestsTitle')}
        {!requestMode && requestCount.data?.count > 0 && (
          <span className="tab-count">{requestCount.data.count}</span>
        )}
      </Link>
    ) : (
      <>
        {[
          ['/groups/new', 'groups:create'],
          ['/groups/invitations', 'groups:invitationsTitle'],
          ['/groups/join', 'groups:joinTitle'],
        ].map(([to, label]) => (
          <Link
            className="text-link"
            key={to}
            to={to}
          >
            {t(label)}
          </Link>
        ))}
      </>
    )

  const threadNotice = (
    <>
      {anchoredBefore && (
        <div
          className="context-history-notice"
          role="status"
        >
          <p>
            {targetUnavailable ? t('messages:contextUnavailable') : t('messages:contextHistory')}
          </p>
          <Button
            variant="link"
            onClick={() => setSearchParams({})}
          >
            {t('messages:backToLatest')}
          </Button>
        </div>
      )}
      {area === 'group' && location.state?.createdGroupId === id && (
        <div className="group-created-notice">
          <p>{t('groups:createdHint')}</p>
          <Link
            className="text-link"
            to={`/groups/${id}/manage#invite-links`}
          >
            {t('groups:inviteCta')}
          </Link>
        </div>
      )}
      {requestMode && (
        <div className="request-notice">
          <p>{t('messages:requestReadOnly')}</p>
          {requestActions}
        </div>
      )}
      {receipts.error && (
        <div
          className="connection-notice"
          role="status"
        >
          {t('messages:receiptError')}
          <Button
            variant="link"
            onClick={receipts.retry}
          >
            {t('messages:retryReceipts')}
          </Button>
        </div>
      )}
    </>
  )

  return (
    <ChatWorkspace
      area={area}
      titleOverride={requestMode ? t('messages:requestsTitle') : undefined}
      user={{
        id: access.userId,
        name: me.data?.displayName || access.username,
      }}
      conversations={conversations}
      selectedId={id}
      selected={selected}
      members={members.data || []}
      membersLoading={members.isPending}
      messages={messageItems}
      listLoading={conversationListQuery.isPending}
      listError={conversationListQuery.error}
      threadLoading={threadLoading}
      threadError={detail.error || history.error}
      membersError={members.error}
      onSelect={(selectedId) => navigate(`${conversationBasePath}/${selectedId}`)}
      onBack={() => navigate(conversationBasePath)}
      onRetryList={conversationListQuery.refetch}
      onRetryThread={retryThread}
      onRetryMembers={members.refetch}
      onSend={send.mutateAsync}
      hasMore={history.hasNextPage}
      loadingMore={history.isFetchingNextPage}
      onLoadMore={() => history.fetchNextPage()}
      status={status}
      onLogout={() => logout.mutate()}
      logoutPending={logout.isPending}
      logoutError={logout.error}
      listActions={listActions}
      detailsExtra={
        <>
          {canManageGroup && <GroupInviteCallout id={id} />}
          {detailsExtra}
        </>
      }
      headerSubtitle={headerSubtitle}
      presenceStatus={requestMode ? undefined : presence.data?.online}
      headerTools={<NotificationPreview />}
      focusMessageId={focusMessageId}
      focusRequestKey={location.key}
      onRead={receipts.onRead}
      onTyping={onTyping}
      composerDisabled={requestMode || !activeGroupMember}
      composerContext={context}
      onCancelCompose={() => setContext(null)}
      threadNotice={threadNotice}
      typingActorIds={typingActorIds}
      renderMessageActions={renderActions}
      renderAttachment={renderLiveAttachment}
      renderMessageContext={renderLiveContext}
      renderMessageReactions={renderLiveReactions}
      renderMessageMeta={renderLiveReceipt}
    />
  )
}
function GroupInviteCallout({ id }) {
  const { t } = useTranslation()
  return (
    <section className="group-invite-cta">
      <Link
        className="text-link"
        to={`/groups/${id}/manage#invite-links`}
      >
        {t('groups:inviteCta')}
      </Link>
      <p className="field-help">{t('groups:inviteHint')}</p>
    </section>
  )
}
