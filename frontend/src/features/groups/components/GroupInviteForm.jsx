import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { UserPicker } from '@/components/forms/UserPicker'
import { groupsApi } from '@/features/groups/api'

export function GroupInviteForm({ id, members, refresh }) {
  const { t } = useTranslation()
  const [selected, setSelected] = useState([])
  return (
    <section className="settings-card">
      <h2>{t('groups:inviteTitle')}</h2>
      <p className="page-description">{t('groups:inviteDescription')}</p>
      <ValidatedForm
        id="invite-user"
        submitDisabled={!selected.length}
        fields={[]}
        schema={z.object({})}
        onSubmit={async () => {
          if (!selected[0]) throw { response: { status: 400 } }
          await groupsApi.invite(id, selected[0].id)
          setSelected([])
          await refresh()
        }}
        submitLabel={t('groups:sendInvite')}
        successMessage={t('groups:inviteSent')}
      >
        <UserPicker
          id="group-invite-user"
          selected={selected}
          onChange={setSelected}
          exclude={members.map((member) => member.userId)}
        />
      </ValidatedForm>
    </section>
  )
}
