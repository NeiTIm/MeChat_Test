import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from './ui/button'
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from './ui/dialog'
import { errorMessage } from '@/lib/errors'

export function ConfirmAction({ label, title, description, onConfirm, destructive = false, disabled = false }) {
  const { t } = useTranslation()
  const [open, setOpen] = useState(false)
  const [pending, setPending] = useState(false)
  const [error, setError] = useState(null)
  async function confirm() { setPending(true); setError(null); try { await onConfirm(); setOpen(false) } catch (error) { setError(error) } finally { setPending(false) } }
  return <Dialog open={open} onOpenChange={(value) => { if (!pending) { setOpen(value); setError(null) } }}><DialogTrigger asChild><Button disabled={disabled} variant={destructive ? 'destructive' : 'outline'}>{label}</Button></DialogTrigger><DialogContent className="details-dialog"><DialogHeader><DialogTitle>{title || label}</DialogTitle><DialogDescription>{description}</DialogDescription></DialogHeader>{error && <p role="alert" className="inline-error">{errorMessage(error)}</p>}<div className="confirmation-actions"><Button variant="outline" disabled={pending} onClick={() => setOpen(false)}>{t('common:cancel')}</Button><Button variant={destructive ? 'destructive' : 'default'} disabled={pending} onClick={confirm}>{pending ? t('common:saving') : t('common:confirm')}</Button></div></DialogContent></Dialog>
}
