import { translate } from './index'

export const DISPLAY_TIME_ZONE = 'Asia/Ho_Chi_Minh'
export const DISPLAY_LOCALE = 'vi-VN'
const date = (value) => { const result = new Date(value); return value && !Number.isNaN(result.getTime()) ? result : null }
export function formatTime(value) {
  const parsed = date(value)
  return parsed ? new Intl.DateTimeFormat(DISPLAY_LOCALE, { timeZone: DISPLAY_TIME_ZONE, hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(parsed) : ''
}
export function formatDate(value, options = {}) {
  const parsed = date(value)
  return parsed ? new Intl.DateTimeFormat(DISPLAY_LOCALE, { timeZone: DISPLAY_TIME_ZONE, day: '2-digit', month: '2-digit', year: 'numeric', ...options }).format(parsed) : ''
}
function dayKey(value) { return new Intl.DateTimeFormat('en-CA', { timeZone: DISPLAY_TIME_ZONE, year: 'numeric', month: '2-digit', day: '2-digit' }).format(value) }
export function formatDay(value, now = new Date()) {
  const parsed = date(value)
  if (!parsed) return ''
  if (dayKey(parsed) === dayKey(now)) return translate('common:today')
  if (dayKey(parsed) === dayKey(new Date(now.getTime() - 86400000))) return translate('common:yesterday')
  return formatDate(value)
}
export function formatRelative(value, now = new Date()) {
  const parsed = date(value)
  if (!parsed) return ''
  const seconds = Math.floor((now.getTime() - parsed.getTime()) / 1000)
  if (seconds < 0) return formatDate(value)
  if (seconds < 60) return translate('common:justNow')
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return translate('common:minutesAgo', { count: minutes })
  const hours = Math.floor(minutes / 60)
  return hours < 24 ? translate('common:hoursAgo', { count: hours }) : formatDate(value)
}
export const formatNumber = (value, options) => new Intl.NumberFormat(DISPLAY_LOCALE, options).format(value)
