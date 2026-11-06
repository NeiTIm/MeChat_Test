import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import common from './locales/vi/common.json'
import auth from './locales/vi/auth.json'
import chat from './locales/vi/chat.json'
import groups from './locales/vi/groups.json'
import design from './locales/vi/design.json'
import errors from './locales/vi/errors.json'
import users from './locales/vi/users.json'
import friends from './locales/vi/friends.json'
import messages from './locales/vi/messages.json'
import files from './locales/vi/files.json'
import notifications from './locales/vi/notifications.json'
import reports from './locales/vi/reports.json'
import appeals from './locales/vi/appeals.json'
import admin from './locales/vi/admin.json'

i18n.use(initReactI18next).init({
  lng: 'vi-VN', fallbackLng: 'vi', supportedLngs: ['vi-VN', 'vi'],
  resources: { vi: { common, auth, chat, groups, design, errors, users, friends, messages, files, notifications, reports, appeals, admin } },
  defaultNS: 'common', ns: ['common', 'auth', 'chat', 'groups', 'design', 'errors', 'users', 'friends', 'messages', 'files', 'notifications', 'reports', 'appeals', 'admin'],
  interpolation: { escapeValue: false }, returnNull: false, initImmediate: false,
})
export const translate = (key, options) => i18n.t(key, options)
export const roleLabel = (role) => i18n.t(`common:roles.${role}`, { defaultValue: i18n.t('common:unknownRole') })
export const statusLabel = (status) => i18n.t(`common:statuses.${status}`, { defaultValue: i18n.t('common:unknownStatus') })
export default i18n
