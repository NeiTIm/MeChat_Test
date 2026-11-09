import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ShieldCheck } from 'lucide-react'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { queryClient } from '@/lib/query-client'
import { roleLabel } from '@/i18n'
import { formatDate } from '@/i18n/format'
import { usersApi } from '@/features/users/api'
import { AvatarEditor } from '@/features/files/AvatarEditor'
import { filesApi } from '@/features/files/api'
import { SettingsCard } from './SettingsCard'

export function ProfileSettings({ profile }) {
  const { t } = useTranslation()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['workspace', 'me'] })
  const username = z
    .string()
    .trim()
    .min(3, t('auth:validation.usernameMin'))
    .max(50, t('auth:validation.usernameMax'))
  const email = z.string().trim().email(t('auth:validation.email'))
  return (
    <>
      <SettingsCard
        title={t('users:profileTitle')}
        description={t('users:profileDescription')}
      >
        <div className="profile-summary">
          <ShieldCheck size={20} />
          <span>{profile.email}</span>
          <span className="role-chip">{roleLabel(profile.systemRole)}</span>
        </div>
        <ValidatedForm
          id="profile"
          schema={z.object({
            displayName: z
              .string()
              .trim()
              .min(1, t('common:required'))
              .max(100, t('users:displayNameLimit')),
            bio: z.string().max(500, t('users:bioLimit')),
          })}
          fields={[
            {
              name: 'displayName',
              label: t('users:displayName'),
              maxLength: 100,
              autoComplete: 'name',
            },
            {
              name: 'bio',
              label: t('users:bio'),
              type: 'textarea',
              maxLength: 500,
              required: false,
            },
          ]}
          defaultValues={{ displayName: profile.displayName, bio: profile.bio || '' }}
          onSubmit={async (values) => {
            await usersApi.update(values)
            await invalidate()
          }}
          successMessage={t('users:profileSaved')}
        />
        <AvatarEditor onSave={filesApi.avatar} />
      </SettingsCard>
      <SettingsCard
        title={t('users:usernameTitle')}
        description={t('users:usernameDescription')}
      >
        <ValidatedForm
          id="username"
          schema={z.object({ newUsername: username })}
          fields={[
            {
              name: 'newUsername',
              label: t('users:newUsername'),
              maxLength: 50,
              autoComplete: 'username',
              helper: profile.lastUsernameChangedAt
                ? t('users:lastUsernameChange', { date: formatDate(profile.lastUsernameChangedAt) })
                : undefined,
            },
          ]}
          defaultValues={{ newUsername: profile.username }}
          onSubmit={async (values) => {
            await usersApi.changeUsername(values)
            await invalidate()
          }}
          successMessage={t('users:usernameSaved')}
        />
      </SettingsCard>
      <SettingsCard
        title={t('users:emailTitle')}
        description={t('users:emailDescription')}
      >
        <ValidatedForm
          id="email-change"
          schema={z.object({ newEmail: email })}
          fields={[
            { name: 'newEmail', label: t('users:newEmail'), type: 'email', autoComplete: 'email' },
          ]}
          onSubmit={usersApi.changeEmail}
          submitLabel={t('users:requestEmailChange')}
          successMessage={t('users:emailRequested')}
          resetOnSuccess
        />
        <div className="section-divider" />
        <ValidatedForm
          id="email-confirm"
          schema={z.object({ token: z.string().trim().min(1, t('common:required')) })}
          fields={[{ name: 'token', label: t('users:emailChangeToken'), autoComplete: 'off' }]}
          onSubmit={async (values) => {
            await usersApi.confirmEmail(values)
            await invalidate()
          }}
          submitLabel={t('users:confirmEmail')}
          successMessage={t('users:emailConfirmed')}
          resetOnSuccess
        />
      </SettingsCard>
    </>
  )
}
