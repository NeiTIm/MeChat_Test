import { useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ApplicationLayout } from '@/components/layout/ApplicationLayout'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { UserPicker } from '@/components/forms/UserPicker'
import { StatePanel } from '@/components/StatePanel'
import { ConfirmAction } from '@/components/ConfirmAction'
import { Avatar } from '@/components/Avatar'
import { Button } from '@/components/ui/button'
import { queryClient } from '@/lib/query-client'
import { useAuthStore } from '@/stores/auth-store'
import { formatDate, formatTime } from '@/i18n/format'
import { roleLabel } from '@/i18n'
import { groupsApi } from './api'
import { AvatarEditor } from '@/features/files/AvatarEditor'

const refresh = () => queryClient.invalidateQueries({ queryKey: ['workspace'] })
const fields = (t) => [{ name: 'name', label: t('groups:name'), maxLength: 100 }, { name: 'description', label: t('groups:description'), type: 'textarea', maxLength: 500, required: false }]
const schema = (t) => z.object({ name: z.string().trim().min(1, t('common:required')).max(100, t('groups:nameLimit')), description: z.string().max(500, t('groups:descriptionLimit')) })
export function GroupCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [selected, setSelected] = useState([])
  return <ApplicationLayout area="group" title={t('groups:createTitle')} description={t('groups:createDescription')} actions={<Link className="text-link" to="/groups">{t('common:back')}</Link>}><section className="settings-card"><ValidatedForm id="create-group" fields={fields(t)} schema={schema(t)} defaultValues={{ name: '', description: '' }} submitLabel={t('groups:create')} onSubmit={async (values) => { const group = await groupsApi.create({ ...values, inviteeUserIds: selected.map((person) => person.id) }); await refresh(); navigate(`/groups/${group.id}`, { state: { createdGroupId: group.id } }) }}><UserPicker id="create-invitees" selected={selected} onChange={setSelected} multiple /></ValidatedForm></section></ApplicationLayout>
}
export function GroupInvitationsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const invitations = useQuery({ queryKey: ['workspace', 'groupInvitations'], queryFn: groupsApi.invitations })
  return <ApplicationLayout area="group" title={t('groups:invitationsTitle')} description={t('groups:invitationsDescription')}><section className="settings-card">{invitations.isPending ? <StatePanel loading /> : invitations.error ? <StatePanel error={invitations.error} onRetry={invitations.refetch} /> : invitations.data.length === 0 ? <StatePanel title={t('groups:noInvitations')} /> : <div className="people-list">{invitations.data.map((invite) => <article className="person-row" key={invite.id}><div><h2>{invite.groupName}</h2><p className="field-help">{formatDate(invite.createdAt)}</p></div><div className="person-actions"><ConfirmAction label={t('groups:acceptInvite')} description={t('groups:acceptDescription')} onConfirm={async () => { await groupsApi.accept(invite.id); await refresh(); navigate(`/groups/${invite.conversationId}`) }} /><ConfirmAction label={t('groups:rejectInvite')} description={t('groups:rejectDescription')} onConfirm={async () => { await groupsApi.reject(invite.id); await refresh() }} /></div></article>)}</div>}</section></ApplicationLayout>
}
export function GroupManagementPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  // GroupChanged resets protected queries and temporarily unmounts GroupLinks.
  // Keep the one-time secret in this route's memory, never in query/storage/logs.
  const [createdLink, setCreatedLink] = useState(null)
  const group = useQuery({ queryKey: ['workspace', 'group', id, 'detail'], queryFn: ({ signal }) => groupsApi.get(id, signal) })
  useEffect(() => { if (group.isError) setCreatedLink(null) }, [group.isError])
  const members = useQuery({ queryKey: ['workspace', 'group', id, 'members'], queryFn: ({ signal }) => groupsApi.members(id, signal) })
  useEffect(() => {
    if (group.data?.status !== 'Active' || group.isPending) return
    const section = window.location.hash === '#invite-links' ? document.getElementById('invite-links') : null
    section?.scrollIntoView({ block: 'start' }); section?.focus({ preventScroll: true })
  }, [group.data, group.isPending])
  return <ApplicationLayout area="group" title={t('groups:manageTitle')} description={group.data?.name} actions={<Link className="text-link" to={`/groups/${id}`}>{t('groups:backToGroup')}</Link>}>
    {group.isPending ? <StatePanel loading /> : group.error ? <StatePanel error={group.error} onRetry={group.refetch} /> : group.data.status !== 'Active' || !['Owner', 'Admin', 'Member'].includes(group.data.myRole) ? <StatePanel title={t('groups:unavailable')} /> : <>
      <section className="settings-card"><h2>{t('groups:profileTitle')}</h2><p className="page-description">{t('groups:profileDescription')}</p><ValidatedForm id="group-profile" key={id} fields={fields(t)} schema={schema(t)} defaultValues={{ name: group.data.name, description: group.data.description || '' }} onSubmit={async (values) => { await groupsApi.update(id, { ...values, avatarFileId: group.data.avatarFileId }); await refresh() }} successMessage={t('groups:profileSaved')} /><AvatarEditor group onSave={async (avatarFileId) => { const current = await groupsApi.get(id); await groupsApi.update(id, { name: current.name, description: current.description, avatarFileId }) }} /></section>
      <GroupInviteForm id={id} members={members.data || []} />
      <section className="settings-card"><h2>{t('groups:membersTitle')}</h2><p className="page-description">{t('groups:rolesDescription')}</p>{members.isPending ? <StatePanel loading /> : members.error ? <StatePanel error={members.error} onRetry={members.refetch} /> : <GroupMembers group={group.data} members={members.data} />}</section>
      <GroupLinks group={group.data} created={createdLink?.groupId === id ? createdLink : null} onCreated={(link) => setCreatedLink(link ? { ...link, groupId: id } : null)} />
      <GroupDanger group={group.data} />
    </>}
  </ApplicationLayout>
}
function GroupInviteForm({ id, members }) {
  const { t } = useTranslation()
  const [selected, setSelected] = useState([])
  return <section className="settings-card"><h2>{t('groups:inviteTitle')}</h2><p className="page-description">{t('groups:inviteDescription')}</p><ValidatedForm id="invite-user" submitDisabled={!selected.length} fields={[]} schema={z.object({})} onSubmit={async () => { if (!selected[0]) throw { response: { status: 400 } }; await groupsApi.invite(id, selected[0].id); setSelected([]); await refresh() }} submitLabel={t('groups:sendInvite')} successMessage={t('groups:inviteSent')}><UserPicker id="group-invite-user" selected={selected} onChange={setSelected} exclude={members.map((member) => member.userId)} /></ValidatedForm></section>
}
function GroupMembers({ group, members }) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  return <div className="people-list">{members.map((member) => {
    const manageable = member.userId !== ownId && member.role !== 'Owner' && (group.myRole === 'Owner' || group.myRole === 'Admin' && member.role === 'Member')
    return <article className="person-row" key={member.userId}><div className="member-management-info"><Avatar name={member.displayName || member.username || '?'} /><div><strong>{member.displayName || member.username || t('groups:member')}</strong><p className="field-help">{roleLabel(member.role)} · {t('groups:joined', { date: formatDate(member.joinedAt) })}</p></div></div><div className="person-actions">{manageable && <><ConfirmAction label={t(member.role === 'Member' ? 'groups:promote' : 'groups:demote')} description={t('groups:roleConfirmation')} onConfirm={async () => { await groupsApi.role(group.id, member.userId, member.role === 'Member' ? 'Admin' : 'Member'); await refresh() }} /><ConfirmAction label={t('groups:removeMember')} description={t('groups:removeConfirmation')} destructive onConfirm={async () => { await groupsApi.remove(group.id, member.userId); await queryClient.resetQueries({ queryKey: ['workspace'] }) }} /></>}{group.myRole === 'Owner' && member.userId !== ownId && <ConfirmAction label={t('groups:transfer')} description={t('groups:transferConfirmation')} onConfirm={async () => { await groupsApi.transfer(group.id, member.userId); await refresh() }} />}</div></article>
  })}</div>
}
function GroupLinks({ group, created, onCreated }) {
  const { t } = useTranslation()
  const ownId = useAuthStore((state) => state.access?.userId)
  const [hours, setHours] = useState('24')
  const [copied, setCopied] = useState(false)
  const [copyError, setCopyError] = useState(false)
  const links = useQuery({ queryKey: ['workspace', 'groupLinks', group.id], queryFn: ({ signal }) => groupsApi.links(group.id, signal) })
  const currentLink = created && links.data?.find((link) => link.id === created.id)
  const shareable = currentLink && (!currentLink.expiresAt || Date.parse(currentLink.expiresAt) > Date.now())
  return <section className="settings-card" id="invite-links" tabIndex={-1}><h2>{t('groups:linksTitle')}</h2><p className="page-description">{t('groups:linksDescription')}</p><ValidatedForm id="create-link" fields={[]} schema={z.object({})} submitLabel={t('groups:createLink')} onSubmit={async () => {
    const result = await groupsApi.createLink(group.id, hours === 'none' ? null : new Date(Date.now() + Number(hours) * 3600000).toISOString())
    if (typeof result?.token !== 'string' || !result.token.trim() || !result.id) throw new Error('INVALID_INVITE_RESPONSE')
    onCreated({ id: result.id, url: `${window.location.origin}/groups/join#token=${encodeURIComponent(result.token)}` })
    setCopied(false); await links.refetch()
  }}><div className="form-field"><label htmlFor="link-expiry">{t('groups:expiry')}</label><select id="link-expiry" value={hours} onChange={(event) => setHours(event.target.value)}>{['1', '24', '168', '720', 'none'].map((value) => <option key={value} value={value}>{t(`groups:expiryOptions.${value}`)}</option>)}</select></div></ValidatedForm>
    {created && shareable && <div className="new-invite-link"><label htmlFor="created-invite-link">{t('groups:createdLink')}</label><input id="created-invite-link" readOnly value={created.url} onFocus={(event) => event.target.select()} /><p className="field-help">{t('groups:linkOnce')}</p><Button variant="outline" onClick={async () => { try { if (currentLink.expiresAt && Date.parse(currentLink.expiresAt) <= Date.now()) throw new Error('EXPIRED_LINK'); await navigator.clipboard.writeText(created.url); setCopied(true); setCopyError(false) } catch { setCopyError(true) } }}>{t(copied ? 'groups:copied' : 'groups:copyLink')}</Button>{copied && <p role="status" className="field-help">{t('groups:copied')}</p>}{copyError && <p role="alert" className="field-error">{t('groups:copyError')}</p>}</div>}
    {created && links.data && !links.isFetching && !shareable && <p className="field-help" role="status">{t('groups:linkUnavailable')}</p>}
    {links.isPending ? <StatePanel loading /> : links.error ? <StatePanel error={links.error} onRetry={links.refetch} /> : !links.data.length ? <StatePanel title={t('groups:noLinks')} /> : links.data.map((link) => <article className="session-row" key={link.id}><div><h3>{t('groups:linkCreated', { date: `${formatDate(link.createdAt)} · ${formatTime(link.createdAt)}` })}</h3><p>{link.expiresAt ? t('groups:linkExpires', { date: `${formatDate(link.expiresAt)} · ${formatTime(link.expiresAt)}` }) : t('groups:noExpiry')}</p><span className="role-chip">{t(link.expiresAt && Date.parse(link.expiresAt) <= Date.now() ? 'groups:linkExpired' : 'groups:linkActive')}</span></div>{(group.myRole === 'Owner' || group.myRole === 'Admin' || link.createdByUserId === ownId) && <ConfirmAction label={t('groups:revokeLink')} description={t('groups:revokeDescription')} onConfirm={async () => { await groupsApi.revokeLink(group.id, link.id); if (created?.id === link.id) onCreated(null); await links.refetch() }} />}</article>)}
  </section>
}
function GroupDanger({ group }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  async function act(method) { await groupsApi[method](group.id); navigate('/groups'); await queryClient.resetQueries({ queryKey: ['workspace'] }) }
  return <section className="settings-card danger-card"><h2>{t('groups:membershipTitle')}</h2><p className="page-description">{t(group.myRole === 'Owner' ? 'groups:ownerLeaveHint' : 'groups:leaveDescription')}</p><div className="section-actions">{group.myRole !== 'Owner' && <ConfirmAction label={t('groups:leave')} description={t('groups:leaveDescription')} destructive onConfirm={() => act('leave')} />}{group.myRole === 'Owner' && <ConfirmAction label={t('groups:dissolve')} description={t('groups:dissolveDescription')} destructive onConfirm={() => act('dissolve')} />}</div></section>
}
export function GroupJoinPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [initial] = useState(() => {
    const token = new URLSearchParams(window.location.hash.slice(1)).get('token') || ''
    return token
  })
  useEffect(() => { if (window.location.hash) window.history.replaceState(window.history.state, '', `${window.location.pathname}${window.location.search}`) }, [])
  const [resolved, setResolved] = useState(null)
  return <ApplicationLayout area="group" title={t('groups:joinTitle')} description={t('groups:joinDescription')}><section className="settings-card"><ValidatedForm id="resolve-link" fields={[{ name: 'token', label: t('groups:inviteCode'), autoComplete: 'off' }]} schema={z.object({ token: z.string().trim().min(1, t('common:required')) })} defaultValues={{ token: initial }} submitLabel={t('groups:previewLink')} onSubmit={async ({ token }) => { setResolved(null); const preview = await groupsApi.resolveLink(token); setResolved({ token, preview }) }} />{resolved && <div className="join-preview"><h2>{resolved.preview.name}</h2><p className="page-description">{resolved.preview.description}</p><ConfirmAction label={t('groups:join')} description={t('groups:joinConfirmation')} onConfirm={async () => { const result = await groupsApi.joinLink(resolved.token); setResolved(null); await refresh(); navigate(`/groups/${result.groupId}`) }} /></div>}</section></ApplicationLayout>
}
