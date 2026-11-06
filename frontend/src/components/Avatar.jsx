import { useQuery } from '@tanstack/react-query'
import { filesApi } from '@/features/files/api'
import { useBlobUrl } from '@/features/files/AttachmentPreview'

export function Avatar({ name = '?', tone = 0, small = false, fileId }) {
  if (fileId) return <AuthorizedAvatar name={name} tone={tone} small={small} fileId={fileId} />
  const initials = name.trim().split(/\s+/).map((part) => part[0]).slice(0, 2).join('').toUpperCase()
  return <span aria-hidden="true" className={`avatar tone-${Number(tone) % 5} ${small ? 'avatar-small' : ''}`}>{initials || '?'}</span>
}
function AuthorizedAvatar({ fileId, ...props }) {
  const preview = useQuery({ queryKey: ['workspace', 'file', fileId], queryFn: ({ signal }) => filesApi.preview(fileId, signal), gcTime: 0 })
  const url = useBlobUrl(preview.data)
  return url && /^image\/(jpeg|png|webp)$/.test(preview.data?.type) ? <img aria-hidden="true" alt="" className={`avatar avatar-photo ${props.small ? 'avatar-small' : ''}`} src={url} /> : <Avatar {...props} />
}
