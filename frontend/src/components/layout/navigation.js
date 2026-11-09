import { MessageSquare, UserRound, Users, ContactRound, Bell } from 'lucide-react'

export function navigationItems(t, { preview = false, area } = {}) {
  const designPreview = import.meta.env.DEV && preview
  return [
    {
      label: t('common:messages'),
      to: designPreview ? '/design/private' : '/chat',
      Icon: MessageSquare,
      active: area === 'private',
    },
    {
      label: t('common:groups'),
      to: designPreview ? '/design/group' : '/groups',
      Icon: Users,
      active: area === 'group',
    },
    ...(!designPreview
      ? [
          {
            label: t('friends:title'),
            to: '/friends',
            Icon: ContactRound,
            active: area === 'friends',
          },
          {
            label: t('notifications:title'),
            to: '/notifications',
            Icon: Bell,
            active: area === 'notifications',
          },
          {
            label: t('common:account'),
            to: '/account',
            Icon: UserRound,
            active: area === 'account',
          },
        ]
      : []),
  ]
}
