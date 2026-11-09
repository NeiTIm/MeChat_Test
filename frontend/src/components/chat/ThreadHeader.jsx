import { ArrowLeft, Info, Search } from 'lucide-react'
import { Avatar } from '@/components/Avatar'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { roleLabel } from '@/i18n'

export function ThreadHeader({
  t,
  onBack,
  title,
  selected,
  group,
  presenceStatus,
  headerSubtitle,
  membersLoading,
  membersError,
  members,
  headerTools,
  searchOpen,
  setSearchOpen,
  setMessageFilter,
  wide,
  detailsTrigger,
  detailsOpen,
  setDetailsOpen,
  details,
}) {
  return (
    <header className="thread-header">
      <Button
        variant="ghost"
        size="icon"
        className="back-button icon-button"
        onClick={onBack}
        aria-label={t('chat:back-to-value0', {
          value0: title.toLowerCase(),
        })}
      >
        <ArrowLeft />
      </Button>
      <Avatar
        fileId={selected.avatarFileId}
        name={selected.name}
        tone={group ? 3 : 1}
        small
      />
      <div className="thread-heading">
        <h1 title={selected.name}>{selected.name}</h1>
        <p>
          {!group && typeof presenceStatus === 'boolean' && (
            <span
              aria-hidden="true"
              className={`presence-dot ${presenceStatus ? 'presence-online' : 'presence-offline'}`}
            />
          )}
          {headerSubtitle ||
            (group
              ? membersLoading || membersError
                ? roleLabel(selected.myRole)
                : t('chat:memberSummary', {
                    count: members.length,
                    role: roleLabel(selected.myRole),
                  })
              : t('chat:private-conversation'))}
        </p>
      </div>
      <div className="thread-actions">
        {headerTools}
        <Button
          variant="ghost"
          size="icon"
          className="icon-button"
          aria-label={t('chat:search-loaded-messages')}
          aria-expanded={searchOpen}
          onClick={() => {
            setSearchOpen(!searchOpen)
            setMessageFilter('')
          }}
        >
          <Search />
        </Button>
        {wide ? (
          <Button
            ref={detailsTrigger}
            variant="ghost"
            size="icon"
            className="icon-button"
            aria-label={t('chat:conversation-details')}
            aria-expanded={detailsOpen}
            onClick={() => setDetailsOpen(!detailsOpen)}
          >
            <Info />
          </Button>
        ) : (
          <Dialog
            open={detailsOpen}
            onOpenChange={setDetailsOpen}
          >
            <DialogTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="icon-button"
                aria-label={t('chat:conversation-details')}
              >
                <Info />
              </Button>
            </DialogTrigger>
            <DialogContent className="details-dialog">
              <DialogHeader>
                <DialogTitle>{t('chat:conversation-details')}</DialogTitle>
                <DialogDescription>
                  {group
                    ? t('chat:about-this-group-and-its-members')
                    : t('chat:about-this-conversation')}
                </DialogDescription>
              </DialogHeader>
              {details}
            </DialogContent>
          </Dialog>
        )}
      </div>
    </header>
  )
}
