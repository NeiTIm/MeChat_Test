import { GroupInviteForm } from '@/features/groups/components/GroupInviteForm'
import { GroupMembers } from '@/features/groups/components/GroupMembers'
import { GroupLinks } from '@/features/groups/components/GroupLinks'
import { GroupDanger } from '@/features/groups/components/GroupDanger'
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
import { queryClient } from '@/lib/query-client'
import { formatDate } from '@/i18n/format'
import { groupsApi } from './api'
import { AvatarEditor } from '@/features/files/AvatarEditor'

const refresh = () => queryClient.invalidateQueries({ queryKey: ['workspace'] })
const fields = (t) => [
  { name: 'name', label: t('groups:name'), maxLength: 100 },
  {
    name: 'description',
    label: t('groups:description'),
    type: 'textarea',
    maxLength: 500,
    required: false,
  },
]
const schema = (t) =>
  z.object({
    name: z.string().trim().min(1, t('common:required')).max(100, t('groups:nameLimit')),
    description: z.string().max(500, t('groups:descriptionLimit')),
  })
export function GroupCreatePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [selected, setSelected] = useState([])
  return (
    <ApplicationLayout
      area="group"
      title={t('groups:createTitle')}
      description={t('groups:createDescription')}
      actions={
        <Link
          className="text-link"
          to="/groups"
        >
          {t('common:back')}
        </Link>
      }
    >
      <section className="settings-card">
        <ValidatedForm
          id="create-group"
          fields={fields(t)}
          schema={schema(t)}
          defaultValues={{ name: '', description: '' }}
          submitLabel={t('groups:create')}
          onSubmit={async (values) => {
            const group = await groupsApi.create({
              ...values,
              inviteeUserIds: selected.map((person) => person.id),
            })
            await refresh()
            navigate(`/groups/${group.id}`, { state: { createdGroupId: group.id } })
          }}
        >
          <UserPicker
            id="create-invitees"
            selected={selected}
            onChange={setSelected}
            multiple
          />
        </ValidatedForm>
      </section>
    </ApplicationLayout>
  )
}
export function GroupInvitationsPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const invitations = useQuery({
    queryKey: ['workspace', 'groupInvitations'],
    queryFn: groupsApi.invitations,
  })
  return (
    <ApplicationLayout
      area="group"
      title={t('groups:invitationsTitle')}
      description={t('groups:invitationsDescription')}
    >
      <section className="settings-card">
        {invitations.isPending ? (
          <StatePanel loading />
        ) : invitations.error ? (
          <StatePanel
            error={invitations.error}
            onRetry={invitations.refetch}
          />
        ) : invitations.data.length === 0 ? (
          <StatePanel title={t('groups:noInvitations')} />
        ) : (
          <div className="people-list">
            {invitations.data.map((invite) => (
              <article
                className="person-row"
                key={invite.id}
              >
                <div>
                  <h2>{invite.groupName}</h2>
                  <p className="field-help">{formatDate(invite.createdAt)}</p>
                </div>
                <div className="person-actions">
                  <ConfirmAction
                    label={t('groups:acceptInvite')}
                    description={t('groups:acceptDescription')}
                    onConfirm={async () => {
                      await groupsApi.accept(invite.id)
                      await refresh()
                      navigate(`/groups/${invite.conversationId}`)
                    }}
                  />
                  <ConfirmAction
                    label={t('groups:rejectInvite')}
                    description={t('groups:rejectDescription')}
                    onConfirm={async () => {
                      await groupsApi.reject(invite.id)
                      await refresh()
                    }}
                  />
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </ApplicationLayout>
  )
}
export function GroupManagementPage() {
  const { t } = useTranslation()
  const { id } = useParams()
  // GroupChanged resets protected queries and temporarily unmounts GroupLinks.
  // Keep the one-time secret in this route's memory, never in query/storage/logs.
  const [createdLink, setCreatedLink] = useState(null)
  const group = useQuery({
    queryKey: ['workspace', 'group', id, 'detail'],
    queryFn: ({ signal }) => groupsApi.get(id, signal),
  })
  useEffect(() => {
    if (group.isError) setCreatedLink(null)
  }, [group.isError])
  const members = useQuery({
    queryKey: ['workspace', 'group', id, 'members'],
    queryFn: ({ signal }) => groupsApi.members(id, signal),
  })
  useEffect(() => {
    if (group.data?.status !== 'Active' || group.isPending) return
    const section =
      window.location.hash === '#invite-links' ? document.getElementById('invite-links') : null
    section?.scrollIntoView({ block: 'start' })
    section?.focus({ preventScroll: true })
  }, [group.data, group.isPending])
  return (
    <ApplicationLayout
      area="group"
      title={t('groups:manageTitle')}
      description={group.data?.name}
      actions={
        <Link
          className="text-link"
          to={`/groups/${id}`}
        >
          {t('groups:backToGroup')}
        </Link>
      }
    >
      {group.isPending ? (
        <StatePanel loading />
      ) : group.error ? (
        <StatePanel
          error={group.error}
          onRetry={group.refetch}
        />
      ) : group.data.status !== 'Active' ||
        !['Owner', 'Admin', 'Member'].includes(group.data.myRole) ? (
        <StatePanel title={t('groups:unavailable')} />
      ) : (
        <>
          <section className="settings-card">
            <h2>{t('groups:profileTitle')}</h2>
            <p className="page-description">{t('groups:profileDescription')}</p>
            <ValidatedForm
              id="group-profile"
              key={id}
              fields={fields(t)}
              schema={schema(t)}
              defaultValues={{ name: group.data.name, description: group.data.description || '' }}
              onSubmit={async (values) => {
                await groupsApi.update(id, { ...values, avatarFileId: group.data.avatarFileId })
                await refresh()
              }}
              successMessage={t('groups:profileSaved')}
            />
            <AvatarEditor
              group
              onSave={async (avatarFileId) => {
                const current = await groupsApi.get(id)
                await groupsApi.update(id, {
                  name: current.name,
                  description: current.description,
                  avatarFileId,
                })
              }}
            />
          </section>
          <GroupInviteForm
            refresh={refresh}
            id={id}
            members={members.data || []}
          />
          <section className="settings-card">
            <h2>{t('groups:membersTitle')}</h2>
            <p className="page-description">{t('groups:rolesDescription')}</p>
            {members.isPending ? (
              <StatePanel loading />
            ) : members.error ? (
              <StatePanel
                error={members.error}
                onRetry={members.refetch}
              />
            ) : (
              <GroupMembers
                refresh={refresh}
                group={group.data}
                members={members.data}
              />
            )}
          </section>
          <GroupLinks
            group={group.data}
            created={createdLink?.groupId === id ? createdLink : null}
            onCreated={(link) => setCreatedLink(link ? { ...link, groupId: id } : null)}
          />
          <GroupDanger group={group.data} />
        </>
      )}
    </ApplicationLayout>
  )
}

export function GroupJoinPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [initial] = useState(() => {
    const token = new URLSearchParams(window.location.hash.slice(1)).get('token') || ''
    return token
  })
  useEffect(() => {
    if (window.location.hash)
      window.history.replaceState(
        window.history.state,
        '',
        `${window.location.pathname}${window.location.search}`,
      )
  }, [])
  const [resolved, setResolved] = useState(null)
  return (
    <ApplicationLayout
      area="group"
      title={t('groups:joinTitle')}
      description={t('groups:joinDescription')}
    >
      <section className="settings-card">
        <ValidatedForm
          id="resolve-link"
          fields={[{ name: 'token', label: t('groups:inviteCode'), autoComplete: 'off' }]}
          schema={z.object({ token: z.string().trim().min(1, t('common:required')) })}
          defaultValues={{ token: initial }}
          submitLabel={t('groups:previewLink')}
          onSubmit={async ({ token }) => {
            setResolved(null)
            const preview = await groupsApi.resolveLink(token)
            setResolved({ token, preview })
          }}
        />
        {resolved && (
          <div className="join-preview">
            <h2>{resolved.preview.name}</h2>
            <p className="page-description">{resolved.preview.description}</p>
            <ConfirmAction
              label={t('groups:join')}
              description={t('groups:joinConfirmation')}
              onConfirm={async () => {
                const result = await groupsApi.joinLink(resolved.token)
                setResolved(null)
                await refresh()
                navigate(`/groups/${result.groupId}`)
              }}
            />
          </div>
        )}
      </section>
    </ApplicationLayout>
  )
}
