import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { ValidatedForm } from '@/components/forms/ValidatedForm'
import { queryClient } from '@/lib/query-client'
import { adminApi } from './api'

export function AdminAction({
  label,
  target,
  description,
  onConfirm,
  field = 'reason',
  hold = false,
  suspension = false,
  destructive = false,
}) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const [pending, setPending] = useState(false)
  const [values, setValues] = useState(null)
  const [duration, setDuration] = useState('7')
  const [finished, setFinished] = useState(false)
  const [activeLabel, setActiveLabel] = useState(label)
  return (
    <Dialog
      open={open}
      onOpenChange={(value) => {
        if (!pending) {
          setOpen(value)
          if (!value) {
            setValues(null)
            setFinished(false)
          }
        }
      }}
    >
      <DialogTrigger asChild>
        <Button
          onClick={() => setActiveLabel(label)}
          variant={destructive ? 'destructive' : 'outline'}
        >
          {label}
        </Button>
      </DialogTrigger>
      <DialogContent className="details-dialog">
        <DialogHeader>
          <DialogTitle>{activeLabel}</DialogTitle>
          <DialogDescription>{description || t('admin:actionDescription')}</DialogDescription>
        </DialogHeader>
        <p className="admin-action-target">{target}</p>
        {finished ? (
          <p
            className="form-feedback feedback-success"
            role="status"
          >
            {t('admin:actionSuccess')}
          </p>
        ) : values ? (
          <>
            <p className="admin-confirm-reason">{values[field]}</p>
            <ValidatedForm
              id="confirm-admin-action"
              fields={[]}
              schema={z.object({})}
              submitLabel={t('admin:confirmAction')}
              destructive={destructive}
              onSubmit={async () => {
                setPending(true)
                try {
                  await onConfirm({
                    ...values,
                    ...(suspension
                      ? {
                          expiresAt:
                            duration === 'none'
                              ? null
                              : new Date(Date.now() + Number(duration) * 86400000).toISOString(),
                        }
                      : {}),
                  })
                  await queryClient.invalidateQueries({ queryKey: ['workspace'] })
                  setFinished(true)
                } finally {
                  setPending(false)
                }
              }}
            />
            <Button
              variant="ghost"
              disabled={pending}
              onClick={() => setValues(null)}
            >
              {t('common:back')}
            </Button>
          </>
        ) : (
          <ValidatedForm
            id="admin-reason"
            fields={[
              {
                name: field,
                label: t(field === 'note' ? 'admin:note' : 'admin:reason'),
                type: 'textarea',
                maxLength: 1000,
              },
            ]}
            schema={z.object({
              [field]: z
                .string()
                .trim()
                .min(hold ? 10 : 1, t(hold ? 'admin:holdReasonMin' : 'common:required'))
                .max(1000, t('admin:reasonLimit')),
            })}
            onSubmit={async (input) => setValues(input)}
            submitLabel={t('common:continue')}
          >
            {suspension && (
              <div className="form-field">
                <label htmlFor="suspension-duration">{t('admin:suspensionDuration')}</label>
                <select
                  id="suspension-duration"
                  value={duration}
                  onChange={(event) => setDuration(event.target.value)}
                >
                  {['1', '7', '30', 'none'].map((value) => (
                    <option
                      key={value}
                      value={value}
                    >
                      {t(`admin:durations.${value}`)}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </ValidatedForm>
        )}
      </DialogContent>
    </Dialog>
  )
}
export function HoldAction({ kind, id, held }) {
  const { t } = useTranslation()
  return (
    <AdminAction
      label={t(held ? 'admin:releaseHold' : 'admin:hold')}
      target={id}
      description={t('admin:holdDescription')}
      hold
      onConfirm={({ reason }) => adminApi.hold(kind, id, { hold: !held, reason })}
    />
  )
}
