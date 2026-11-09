import { useTranslation } from 'react-i18next'
import { BarChart3, Flag, MessageSquare, Users } from 'lucide-react'
import { formatNumber } from '@/i18n/format'

export function MetricCards({ data, namespace = 'admin:metrics' }) {
  const { t } = useTranslation()
  const icons = [Users, MessageSquare, BarChart3, Flag]
  return (
    <div className="admin-metrics">
      {Object.entries(data).map(([key, value], index) => {
        const Icon = icons[index % icons.length]
        return (
          <article key={key}>
            <span className="metric-icon">
              <Icon size={21} />
            </span>
            <p>{t(`${namespace}.${key}`)}</p>
            <strong>{formatNumber(value)}</strong>
          </article>
        )
      })}
    </div>
  )
}
