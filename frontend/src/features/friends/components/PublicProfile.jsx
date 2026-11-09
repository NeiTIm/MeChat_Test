import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { Avatar } from '@/components/Avatar'
import { StatePanel } from '@/components/StatePanel'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { usersApi } from '@/features/users/api'
import { ReportDialog } from '@/features/moderation/components/ReportDialog'

export function PublicProfile({ id, fallback }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const profile = useQuery({
    queryKey: ['workspace', 'profile', id],
    queryFn: ({ signal }) => usersApi.profile(id, signal),
    enabled: open,
  })
  return (
    <Dialog
      open={open}
      onOpenChange={setOpen}
    >
      <DialogTrigger asChild>
        <button
          type="button"
          className="person-identity"
        >
          <Avatar
            fileId={fallback.avatarFileId}
            name={fallback.displayName || fallback.username || '?'}
          />
          <span>
            <strong>
              {fallback.displayName || fallback.username || t('friends:profileTitle')}
            </strong>
            {fallback.username && <small>@{fallback.username}</small>}
          </span>
        </button>
      </DialogTrigger>
      <DialogContent className="details-dialog">
        <DialogHeader>
          <DialogTitle>{t('friends:profileTitle')}</DialogTitle>
          <DialogDescription>{t('friends:profileDescription')}</DialogDescription>
        </DialogHeader>
        {profile.isPending ? (
          <StatePanel loading />
        ) : profile.error ? (
          <StatePanel
            error={profile.error}
            onRetry={profile.refetch}
          />
        ) : (
          <div className="public-profile">
            <Avatar
              fileId={profile.data.avatarFileId}
              name={profile.data.displayName}
            />
            <h2>{profile.data.displayName}</h2>
            <p>@{profile.data.username}</p>
            <p>{profile.data.bio || t('friends:noBio')}</p>
            <ReportDialog
              targetType="User"
              targetId={id}
            />
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
