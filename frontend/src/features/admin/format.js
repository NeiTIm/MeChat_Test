import { translate, statusLabel } from '@/i18n'
import { formatNumber } from '@/i18n/format'

// Translate machine-generated presentation while preserving authored reasons/evidence.
export function auditDetails(entry) {
  if (entry.action === 'RetentionDryRun') {
    const fields = entry.details?.match(/^offset=(\d+);limit=(\d+);no deletion$/)
    return fields ? translate('admin:dryRunDetails', { offset: formatNumber(Number(fields[1])), limit: formatNumber(Number(fields[2])) }) : translate('admin:dryRunSummary')
  }
  if (entry.action === 'ReviewAppeal') {
    const fields = entry.details?.match(/^(Accepted|Rejected):\s*([\s\S]*)$/)
    if (fields) return `${statusLabel(fields[1])} · ${fields[2]}`
  }
  return entry.details || ''
}
