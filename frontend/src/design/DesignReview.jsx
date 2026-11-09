import {
  sampleUser,
  people,
  sampleGroup,
  groupMembers,
  sampleMessage,
  privateMessages,
  groupMessages,
} from '@/design/fixtures'
import { useTranslation } from 'react-i18next'
// DEVELOPMENT-ONLY DESIGN FIXTURES. Never used by API adapters or shipped in production.
import { useState } from 'react'
import { Link, NavLink } from 'react-router'
import {
  ArrowUpRight,
  Check,
  CheckCheck,
  FlaskConical,
  MessageSquarePlus,
  Pin,
  Undo2,
} from 'lucide-react'
import { ChatWorkspace } from '@/components/chat/ChatWorkspace'
import { LoginPage } from '@/features/auth/LoginPage'
import { LoginPreviewTools } from './LoginPreviewTools'
import { GroupPreviewTools } from './GroupPreviewTools'
import { MessagePreviewActions } from './MessagePreviewActions'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

export default function DesignReview({ screen }) {
  const { t } = useTranslation()
  const [loginState, setLoginState] = useState('populated')
  return (
    <div className="design-review">
      <header className="review-toolbar">
        <span className="review-label">
          <FlaskConical size={16} />
          <strong>{t('design:design-preview')}</strong>
          <span>{t('design:sample-content-awaiting-approval')}</span>
        </span>
        <nav aria-label={t('design:design-samples')}>
          <NavLink to="/design/login">{t('design:login')}</NavLink>
          <NavLink to="/design/private">{t('design:private-chat')}</NavLink>
          <NavLink to="/design/group">{t('design:group-chat')}</NavLink>
        </nav>
        <Link
          className="review-exit"
          to="/login"
        >
          {t('design:live-sign-in')}
          <ArrowUpRight size={14} />
        </Link>
      </header>
      {screen === 'login' ? (
        <>
          <div className="review-state-control">
            <label htmlFor="login-state">{t('design:preview-state')}</label>
            <select
              id="login-state"
              value={loginState}
              onChange={(event) => setLoginState(event.target.value)}
            >
              <option value="populated">{t('design:ready-to-sign-in')}</option>
              <option value="loading">{t('design:loading')}</option>
              <option value="error">{t('design:sign-in-error')}</option>
            </select>
          </div>
          <LoginPage
            preview
            previewState={loginState}
            secondaryActions={<LoginPreviewTools />}
          />
        </>
      ) : (
        <SampleWorkspace
          key={screen}
          area={screen === 'group' ? 'group' : 'private'}
        />
      )}
    </div>
  )
}
function SampleWorkspace({ area }) {
  const { t } = useTranslation()
  const group = area === 'group'
  const list = group
    ? [
        sampleGroup,
        {
          ...sampleGroup,
          id: 'design-group-2',
          name: 'Hẹn cuối tuần',
          subtitle: 'Chuyến đi tiếp theo bắt đầu từ đây.',
          time: 'Hôm qua',
          unread: 0,
          tone: 2,
        },
      ]
    : people
  const [selectedId, setSelectedId] = useState(list[0].id)
  const [sent, setSent] = useState({})
  const [screenState, setScreenState] = useState('populated')
  const [changes, setChanges] = useState({})
  const [context, setContext] = useState(null)
  const [recallTarget, setRecallTarget] = useState(null)
  const [pinOpen, setPinOpen] = useState(false)
  const [role, setRole] = useState('Member')
  const [notice, setNotice] = useState('')
  const [historyPages, setHistoryPages] = useState(1)
  const selected = list.find((item) => item.id === selectedId)
  const regular = group ? groupMessages : selectedId === people[0].id ? privateMessages : []
  const longHistory = Array.from(
    {
      length: 45,
    },
    (_, index) => ({
      ...sampleMessage(
        `history-${index}`,
        index % 3 === 0 ? sampleUser.id : 'design-jamie',
        `Tin nhắn minh họa số ${index + 1}. Câu chuyện từ phần lịch sử trước.`,
        index + 1,
        '30',
      ),
      previewCanEdit: false,
    }),
  )
  const initial = screenState === 'history' ? longHistory.slice(-15 * historyPages) : regular
  const messages =
    screenState === 'empty'
      ? []
      : [...initial, ...(sent[selectedId] || [])].map((message) => ({
          ...message,
          ...changes[message.id],
        }))
  function changeMessage(id, patch) {
    setChanges((current) => ({
      ...current,
      [id]: {
        ...current[id],
        ...patch,
      },
    }))
  }
  function action(name, message) {
    if (name === 'reply' || name === 'edit') {
      setContext({
        ...message,
        mode: name,
      })
      return
    }
    if (name === 'react') {
      changeMessage(message.id, {
        previewReaction: !message.previewReaction,
      })
      setNotice(
        message.previewReaction
          ? t('design:sample-reaction-removed')
          : t('design:sample-heart-reaction-added'),
      )
      return
    }
    if (name === 'recall') setRecallTarget(message)
  }
  async function send(input) {
    if (context?.mode === 'edit') {
      changeMessage(context.id, {
        content: input.content,
        editedAt: new Date().toISOString(),
      })
      setContext(null)
      setNotice(t('design:sample-edit-saved-locally'))
      return
    }
    setSent((current) => ({
      ...current,
      [selectedId]: [
        ...(current[selectedId] || []),
        {
          ...sampleMessage(
            crypto.randomUUID(),
            sampleUser.id,
            input.content,
            Math.max(0, ...messages.map((message) => message.sequence)) + 1,
            '59',
          ),
          createdAt: new Date().toISOString(),
          previewCanEdit: true,
          ...(context
            ? {
                previewReply: context.content,
              }
            : {}),
        },
      ],
    }))
    setContext(null)
    setNotice(t('design:sample-message-added-locally'))
  }
  function receive() {
    setSent((current) => ({
      ...current,
      [selectedId]: [
        ...(current[selectedId] || []),
        {
          ...sampleMessage(
            crypto.randomUUID(),
            'design-jamie',
            'Có tin nhắn minh họa mới. Vị trí bạn đang đọc vẫn được giữ nguyên.',
            Math.max(0, ...messages.map((message) => message.sequence)) + 1,
            '59',
          ),
          createdAt: new Date().toISOString(),
        },
      ],
    }))
    setNotice(t('design:incoming-message-simulated-locally-no-realtime-event-was-received'))
  }
  return (
    <>
      <div className="review-state-control">
        <label htmlFor="design-state">{t('design:preview-state')}</label>
        <select
          id="design-state"
          value={screenState}
          onChange={(event) => {
            setScreenState(event.target.value)
            setContext(null)
          }}
        >
          <option value="populated">{t('design:populated')}</option>
          <option value="empty">{t('design:empty-history')}</option>
          <option value="loading">{t('design:loading')}</option>
          <option value="error">{t('design:network-error')}</option>
          <option value="permission">{t('design:access-unavailable')}</option>
          <option value="reconnecting">{t('design:reconnecting')}</option>
          <option value="delivered">{t('design:delivered-receipt')}</option>
          <option value="history">{t('design:long-history')}</option>
        </select>
        <Button
          variant="ghost"
          onClick={receive}
          disabled={!selectedId || !['populated', 'history'].includes(screenState)}
          title={t('design:simulate-incoming-sample-message')}
        >
          <MessageSquarePlus size={15} />
          <span>{t('design:simulate-incoming')}</span>
        </Button>
      </div>
      <div
        className="sr-only"
        role="status"
        aria-live="polite"
      >
        {notice}
      </div>
      <ChatWorkspace
        preview
        area={area}
        user={sampleUser}
        conversations={list}
        selectedId={selectedId}
        selected={
          selected && {
            ...selected,
            ...(group
              ? {
                  myRole: role,
                }
              : {}),
          }
        }
        messages={messages}
        members={
          group
            ? groupMembers.map((member) =>
                member.userId === sampleUser.id
                  ? {
                      ...member,
                      role,
                    }
                  : role === 'Owner' && member.role === 'Owner'
                    ? {
                        ...member,
                        role: 'Admin',
                      }
                    : member,
              )
            : []
        }
        threadLoading={screenState === 'loading'}
        threadError={
          screenState === 'error'
            ? new Error('design network fixture')
            : screenState === 'permission'
              ? {
                  response: {
                    status: 404,
                  },
                }
              : null
        }
        onRetryThread={() => setScreenState('populated')}
        onSelect={(id) => {
          setSelectedId(id)
          setContext(null)
        }}
        onBack={() => {
          setSelectedId(null)
          setContext(null)
        }}
        status={screenState === 'reconnecting' ? 'reconnecting' : 'connected'}
        onSend={send}
        composerContext={context}
        onCancelCompose={() => setContext(null)}
        hasMore={screenState === 'history' && historyPages < 3}
        onLoadMore={() => setHistoryPages((pages) => pages + 1)}
        renderMessageActions={(message, own) => (
          <MessagePreviewActions
            message={message}
            own={own}
            onAction={action}
          />
        )}
        renderMessageMeta={(message, own) =>
          own && (
            <span className="receipt-preview">
              {screenState === 'delivered' ? (
                <>
                  <Check size={13} />
                  {t('design:sample-delivered')}
                </>
              ) : (
                <>
                  <CheckCheck size={13} />
                  {t('design:sample-seen')}
                </>
              )}
            </span>
          )
        }
        detailsExtra={
          group ? (
            <GroupPreviewTools
              role={role}
              onRoleChange={setRole}
            />
          ) : (
            <p className="prototype-explanation">
              {t(
                'design:reply-eligible-edit-recall-and-reactions-can-be-explored-from-a-message-s-options-all-sample-changes-stay-in-this-page',
              )}
            </p>
          )
        }
        pinnedMessage={group && messages.length > 0 ? initial[0] : null}
        onPinnedMessage={() => setPinOpen(true)}
      />
      <Dialog
        open={!!recallTarget}
        onOpenChange={(open) => {
          if (!open) setRecallTarget(null)
        }}
      >
        <DialogContent className="details-dialog">
          <DialogHeader>
            <DialogTitle>{t('design:recall-this-sample-message')}</DialogTitle>
            <DialogDescription>
              {t(
                'design:design-preview-this-only-replaces-the-local-sample-content-no-real-message-will-change',
              )}
            </DialogDescription>
          </DialogHeader>
          <div className="confirmation-actions">
            <Button
              variant="outline"
              onClick={() => setRecallTarget(null)}
            >
              {t('design:keep-message')}
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                changeMessage(recallTarget.id, {
                  recalled: true,
                  content: null,
                  previewReaction: false,
                })
                setRecallTarget(null)
                setNotice(t('design:sample-message-recalled-locally'))
              }}
            >
              <Undo2 />
              {t('design:recall-sample')}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
      <Dialog
        open={pinOpen}
        onOpenChange={setPinOpen}
      >
        <DialogContent className="details-dialog">
          <DialogHeader>
            <DialogTitle>{t('design:pinned-messages')}</DialogTitle>
            <DialogDescription>
              {t(
                'design:design-preview-sample-pin-not-a-channel-real-pins-require-group-permission-and-api-integration',
              )}
            </DialogDescription>
          </DialogHeader>
          <div className="pinned-card">
            <Pin size={18} />
            <p>{initial[0]?.content}</p>
            <small>{t('design:pinned-by-jamie-chen-sample')}</small>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
