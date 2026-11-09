import { useTranslation } from 'react-i18next'
import { Flag } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { ReportForm } from './ReportForm'

export function ReportDialog({ targetType, targetId }) {
  const { t } = useTranslation()
  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="outline">
          <Flag size={16} />
          {t('reports:report')}
        </Button>
      </DialogTrigger>
      <DialogContent className="details-dialog">
        <DialogHeader>
          <DialogTitle>{t('reports:report')}</DialogTitle>
          <DialogDescription>{t('reports:reportDescription')}</DialogDescription>
        </DialogHeader>
        <ReportForm
          targetType={targetType}
          targetId={targetId}
        />
      </DialogContent>
    </Dialog>
  )
}
