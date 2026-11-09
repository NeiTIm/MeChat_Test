import { useTranslation } from 'react-i18next'
import { useState } from 'react'
import { Link2, MailPlus, Settings2 } from 'lucide-react'
import { PreviewDialog } from './PreviewDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
export function GroupPreviewTools({ role, onRoleChange }) {
  const { t } = useTranslation()
  return (
    <div className="group-preview-tools">
      <span className="prototype-label">{t('groups:design-preview-group-actions')}</span>
      <PreviewDialog
        title={t('groups:invite-someone-to-the-corner')}
        trigger={
          <Button variant="outline">
            <MailPlus />
            {t('groups:invitations')}
          </Button>
        }
      >
        <p className="prototype-explanation">
          {t(
            'groups:the-real-flow-searches-eligible-users-sends-an-invitation-and-waits-for-their-acceptance-this-sample-only-changes-the-local-invitation-state',
          )}
        </p>
        <InvitationPreview />
      </PreviewDialog>
      <PreviewDialog
        title={t('groups:group-permissions')}
        trigger={
          <Button variant="outline">
            <Settings2 />
            {t('groups:group-actions')}
          </Button>
        }
      >
        <label htmlFor="preview-role">{t('groups:review-as-group-role')}</label>
        <select
          id="preview-role"
          value={role}
          onChange={(event) => onRoleChange(event.target.value)}
        >
          <option value="Member">{t('groups:member')}</option>
          <option value="Admin">{t('groups:admin')}</option>
          <option value="Owner">{t('groups:owner')}</option>
        </select>
        <ul className="permission-list">
          <li>{t('groups:members-can-participate-and-view-accessible-history')}</li>
          <li>{t('groups:admins-can-manage-eligible-members-and-invite-people')}</li>
          <li>{t('groups:only-the-owner-can-transfer-ownership-or-dissolve-the-group')}</li>
        </ul>
        <p className="prototype-explanation">
          {t(
            'groups:role-selection-is-a-design-fixture-it-never-changes-your-real-account-or-group-role-destructive-group-actions-remain-outside-this-review',
          )}
        </p>
      </PreviewDialog>
    </div>
  )
}
function InvitationPreview() {
  const { t } = useTranslation()
  const [state, setState] = useState('idle')
  return state === 'sent' ? (
    <div
      className="preview-success"
      role="status"
    >
      <MailPlus />
      <h3>{t('groups:sample-invitation-pending')}</h3>
      <p>{t('groups:no-real-invitation-was-sent')}</p>
      <Button
        variant="outline"
        onClick={() => setState('idle')}
      >
        {t('groups:reset-sample-invitation')}
      </Button>
    </div>
  ) : (
    <form
      className="preview-form"
      onSubmit={(event) => {
        event.preventDefault()
        setState('sent')
      }}
    >
      <label htmlFor="preview-invite-name">{t('groups:sample-username')}</label>
      <Input
        id="preview-invite-name"
        required
        minLength={3}
        placeholder={t('groups:enter-a-sample-username')}
      />
      <Button type="submit">
        <Link2 />
        {t('groups:preview-invitation')}
      </Button>
    </form>
  )
}
