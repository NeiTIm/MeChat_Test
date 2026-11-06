import { useState } from 'react'
import { Link, NavLink, useNavigate, useParams } from 'react-router'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { MonitorSmartphone, ShieldCheck } from 'lucide-react'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { StatePanel } from '@/components/StatePanel'
import { ConfirmAction } from '@/components/ConfirmAction'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog'
import { authApi } from '@/features/auth/api'
import { authSchemas } from '@/features/auth/schemas'
import { broadcastLogout } from '@/app/providers/AuthProvider'
import { clearSession } from '@/lib/http'
import { queryClient } from '@/lib/query-client'
import { roleLabel } from '@/i18n'
import { formatDate, formatTime } from '@/i18n/format'
import { errorMessage } from '@/lib/errors'
import { usersApi } from './api'
import { AvatarEditor } from '@/features/files/AvatarEditor'
import { filesApi } from '@/features/files/api'

const tabs = ['profile', 'privacy', 'security', 'sessions']
export function AccountPage() {
  const { t } = useTranslation()
  const { section = 'profile' } = useParams()
  const me = useQuery({ queryKey: ['workspace', 'me'], queryFn: usersApi.me })
  return <ApplicationLayout area="account" title={t('users:accountTitle')} description={t('users:accountDescription')}><div className="settings-layout"><nav className="settings-tabs" aria-label={t('users:settingsNavigation')}>{tabs.map((tab) => <NavLink to={`/account/${tab}`} key={tab} className={section === tab ? 'active' : ''}>{t(`users:tabs.${tab}`)}</NavLink>)}</nav><section className="settings-content">
    {me.isPending ? <StatePanel loading /> : me.error ? <StatePanel error={me.error} onRetry={me.refetch} /> : section === 'profile' ? <ProfileSettings profile={me.data} /> : section === 'privacy' ? <PrivacySettings /> : section === 'security' ? <SecuritySettings /> : section === 'sessions' ? <SessionSettings /> : <StatePanel title={t('common:nothing-here-just-yet')} description={t('users:invalidSection')} />}
  </section></div><div className="account-service-links"><Link to="/reports">{t('reports:title')}</Link><Link to="/appeals">{t('appeals:title')}</Link>{me.data?.systemRole === 'Admin' && <Link to="/admin">{t('common:admin')}</Link>}</div></ApplicationLayout>
}
function SettingsCard({ title, description, children, danger }) {
  return <section className={`settings-card ${danger ? 'danger-card' : ''}`}><h2>{title}</h2>{description && <p className="page-description">{description}</p>}{children}</section>
}
function ProfileSettings({ profile }) {
  const { t } = useTranslation()
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['workspace', 'me'] })
  const username = z.string().trim().min(3, t('auth:validation.usernameMin')).max(50, t('auth:validation.usernameMax'))
  const email = z.string().trim().email(t('auth:validation.email'))
  return <>
    <SettingsCard title={t('users:profileTitle')} description={t('users:profileDescription')}><div className="profile-summary"><ShieldCheck size={20} /><span>{profile.email}</span><span className="role-chip">{roleLabel(profile.systemRole)}</span></div>
      <ValidatedForm id="profile" schema={z.object({ displayName: z.string().trim().min(1, t('common:required')).max(100, t('users:displayNameLimit')), bio: z.string().max(500, t('users:bioLimit')) })} fields={[{ name: 'displayName', label: t('users:displayName'), maxLength: 100, autoComplete: 'name' }, { name: 'bio', label: t('users:bio'), type: 'textarea', maxLength: 500, required: false }]} defaultValues={{ displayName: profile.displayName, bio: profile.bio || '' }} onSubmit={async (values) => { await usersApi.update(values); await invalidate() }} successMessage={t('users:profileSaved')} /><AvatarEditor onSave={filesApi.avatar} />
    </SettingsCard>
    <SettingsCard title={t('users:usernameTitle')} description={t('users:usernameDescription')}><ValidatedForm id="username" schema={z.object({ newUsername: username })} fields={[{ name: 'newUsername', label: t('users:newUsername'), maxLength: 50, autoComplete: 'username', helper: profile.lastUsernameChangedAt ? t('users:lastUsernameChange', { date: formatDate(profile.lastUsernameChangedAt) }) : undefined }]} defaultValues={{ newUsername: profile.username }} onSubmit={async (values) => { await usersApi.changeUsername(values); await invalidate() }} successMessage={t('users:usernameSaved')} /></SettingsCard>
    <SettingsCard title={t('users:emailTitle')} description={t('users:emailDescription')}><ValidatedForm id="email-change" schema={z.object({ newEmail: email })} fields={[{ name: 'newEmail', label: t('users:newEmail'), type: 'email', autoComplete: 'email' }]} onSubmit={usersApi.changeEmail} submitLabel={t('users:requestEmailChange')} successMessage={t('users:emailRequested')} resetOnSuccess /><div className="section-divider" /><ValidatedForm id="email-confirm" schema={z.object({ token: z.string().trim().min(1, t('common:required')) })} fields={[{ name: 'token', label: t('users:emailChangeToken'), autoComplete: 'off' }]} onSubmit={async (values) => { await usersApi.confirmEmail(values); await invalidate() }} submitLabel={t('users:confirmEmail')} successMessage={t('users:emailConfirmed')} resetOnSuccess /></SettingsCard>
  </>
}
function PrivacySettings() {
  const privacy = useQuery({ queryKey: ['workspace', 'privacy'], queryFn: usersApi.privacy })
  if (privacy.isPending) return <StatePanel loading />
  if (privacy.error) return <StatePanel error={privacy.error} onRetry={privacy.refetch} />
  return <PrivacyForm initial={privacy.data} />
}
function PrivacyForm({ initial }) {
  const { t } = useTranslation()
  const [values, setValues] = useState(initial)
  const mutation = useMutation({ mutationFn: usersApi.updatePrivacy, onSuccess: () => queryClient.invalidateQueries({ queryKey: ['workspace'] }) })
  return <SettingsCard title={t('users:privacyTitle')} description={t('users:privacyDescription')}><form onSubmit={(event) => { event.preventDefault(); mutation.mutate(values) }}><fieldset className="login-fields" disabled={mutation.isPending}>{['showOnline', 'showLastSeen', 'allowStrangerMessages', 'allowGroupInvites'].map((name) => <label className="privacy-row" key={name}><span><strong>{t(`users:privacy.${name}.title`)}</strong><small>{t(`users:privacy.${name}.description`)}</small></span><input type="checkbox" checked={!!values[name]} onChange={(event) => setValues({ ...values, [name]: event.target.checked })} /></label>)}<Button type="submit" disabled={mutation.isPending}>{mutation.isPending ? t('common:saving') : t('common:save')}</Button></fieldset>{mutation.error && <p className="form-feedback feedback-error" role="alert">{errorMessage(mutation.error)}</p>}{mutation.isSuccess && <p className="form-feedback feedback-success" role="status">{t('users:privacySaved')}</p>}</form></SettingsCard>
}
function SecuritySettings() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  return <><SettingsCard title={t('auth:changePasswordTitle')} description={t('auth:changePasswordDescription')}><ValidatedForm id="change-password" schema={authSchemas(t).changePassword} fields={['currentPassword', 'newPassword', 'confirmPassword'].map((name) => ({ name, label: t(`auth:fields.${name}`), type: 'password', autoComplete: name === 'currentPassword' ? 'current-password' : 'new-password' }))} onSubmit={async ({ confirmPassword: _confirm, ...input }) => { await authApi.changePassword(input); queryClient.invalidateQueries({ queryKey: ['workspace', 'sessions'] }) }} submitLabel={t('auth:changePassword')} successMessage={t('auth:passwordChanged')} resetOnSuccess /></SettingsCard>
    <SettingsCard danger title={t('auth:deactivateTitle')} description={t('auth:deactivateDescription')}><Dialog><DialogTrigger asChild><Button variant="destructive">{t('auth:deactivate')}</Button></DialogTrigger><DialogContent className="details-dialog"><DialogHeader><DialogTitle>{t('auth:deactivateTitle')}</DialogTitle><DialogDescription>{t('auth:deactivateConfirmation')}</DialogDescription></DialogHeader><ValidatedForm id="deactivate" schema={authSchemas(t).deactivate} fields={[{ name: 'password', label: t('auth:fields.password'), type: 'password', autoComplete: 'current-password' }]} onSubmit={async (input) => { await authApi.deactivate(input); broadcastLogout(); navigate('/login', { replace: true }) }} submitLabel={t('auth:deactivate')} destructive /></DialogContent></Dialog></SettingsCard></>
}
function SessionSettings() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const sessions = useQuery({ queryKey: ['workspace', 'sessions'], queryFn: authApi.sessions })
  if (sessions.isPending) return <StatePanel loading />
  if (sessions.error) return <StatePanel error={sessions.error} onRetry={sessions.refetch} />
  async function revoke(session) { await authApi.revokeSession(session.id); if (session.isCurrent) { clearSession(); broadcastLogout(); navigate('/login', { replace: true }) } else await sessions.refetch() }
  return <SettingsCard title={t('auth:sessionsTitle')} description={t('auth:sessionsDescription')}><div className="section-actions"><ConfirmAction label={t('auth:logoutOthers')} description={t('auth:logoutOthersConfirmation')} onConfirm={async () => { await authApi.logoutOthers(); await sessions.refetch() }} /></div>{sessions.data.length === 0 ? <StatePanel compact title={t('auth:noSessions')} /> : <div className="session-list">{sessions.data.map((session) => <article className="session-row" key={session.id}><MonitorSmartphone /><div><h3>{session.deviceName || t('common:unknownDevice')}{session.isCurrent && <span className="role-chip">{t('auth:currentSession')}</span>}</h3><p>{t('auth:sessionCreated', { date: `${formatDate(session.createdAt)} · ${formatTime(session.createdAt)}` })}</p><p>{t('auth:sessionActive', { date: `${formatDate(session.lastActivityAt)} · ${formatTime(session.lastActivityAt)}` })}</p></div><ConfirmAction label={session.isCurrent ? t('auth:signOut') : t('auth:revokeSession')} description={t('auth:revokeConfirmation')} onConfirm={() => revoke(session)} /></article>)}</div>}</SettingsCard>
}
