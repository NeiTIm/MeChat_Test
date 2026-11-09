import { translate } from '@/i18n'
export const IMAGE_EXTENSIONS = ['jpg', 'jpeg', 'png', 'webp']
export const FILE_ACCEPT = '.jpg,.jpeg,.png,.webp,.pdf,.docx,.xlsx,.txt,.zip'
export function validateUpload(file, purpose = 'Message') {
  const ext = file.name.split('.').at(-1).toLowerCase()
  const image = IMAGE_EXTENSIONS.includes(ext)
  if (
    !file.name ||
    file.name.length > 255 ||
    [...file.name].some(
      (character) => character.charCodeAt(0) < 32 || character.charCodeAt(0) === 127,
    )
  )
    return translate('files:invalidName')
  if (!FILE_ACCEPT.split(',').includes(`.${ext}`) || (purpose === 'Avatar' && !image))
    return translate(purpose === 'Avatar' ? 'files:avatarType' : 'files:invalidType')
  const limit = purpose === 'Avatar' ? 5 : image ? 10 : 25
  if (file.size <= 0) return translate('files:emptyFile')
  if (file.size > limit * 1024 * 1024) return translate('files:sizeLimit', { limit })
  return null
}
