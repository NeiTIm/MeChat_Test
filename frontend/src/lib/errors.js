import { translate } from '@/i18n'
const codes = {
  INVALID_CREDENTIALS_OR_ACCOUNT_STATE: 'invalidCredentials', INVALID_REFRESH: 'sessionEnded',
  INVALID_TOKEN: 'invalidToken', USERNAME_UNAVAILABLE_OR_COOLDOWN: 'usernameUnavailable',
  REGISTRATION_UNAVAILABLE: 'registrationUnavailable', EMAIL_UNAVAILABLE: 'emailUnavailable',
  INVALID_CURRENT_PASSWORD: 'currentPassword', REQUEST_OR_FRIENDSHIP_EXISTS: 'friendConflict',
}
export function errorMessage(error) {
  if (typeof navigator !== 'undefined' && !navigator.onLine) return translate('errors:offline')
  if (error?.code === 'ECONNABORTED' || error?.code === 'ETIMEDOUT') return translate('errors:timeout')
  if (!error?.response) return translate('errors:network')
  const { status, data } = error.response
  if (codes[data?.code]) return translate(`errors:${codes[data.code]}`)
  return translate(`errors:${[400, 401, 403, 404, 409, 429].includes(status) ? status : 'server'}`)
}
