import { useTranslation } from 'react-i18next'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
export function PreviewDialog({ trigger, title, description, children }) {
  const { t } = useTranslation()
  return (
    <Dialog>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent className="details-dialog preview-dialog">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>
            {description || t('design:interactive-design-preview-no-backend-request-is-sent')}
          </DialogDescription>
        </DialogHeader>
        <span className="prototype-label">{t('design:design-preview-local-only')}</span>
        {children}
      </DialogContent>
    </Dialog>
  )
}
