import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useTranslation } from 'react-i18next'
import { Eye, EyeOff, LoaderCircle } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { errorMessage } from '@/lib/errors'

export function PasswordInput({ id, ...props }) {
  const [visible, setVisible] = useState(false)
  const { t } = useTranslation('auth')
  return <div className="password-wrap"><Input id={id} type={visible ? 'text' : 'password'} {...props} /><Button type="button" variant="ghost" size="icon" className="password-toggle" onClick={() => setVisible(!visible)} aria-label={t(visible ? 'hide-password' : 'show-password')}>{visible ? <EyeOff /> : <Eye />}</Button></div>
}

export function ValidatedForm({ id, fields, schema, defaultValues = {}, onSubmit, submitLabel, successMessage, resetOnSuccess = false, children, destructive = false, submitDisabled = false }) {
  const { t } = useTranslation()
  const [feedback, setFeedback] = useState(null)
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm({ resolver: zodResolver(schema), defaultValues })
  async function submit(values) {
    setFeedback(null)
    try {
      await onSubmit(values)
      if (resetOnSuccess) reset()
      if (successMessage) setFeedback({ success: successMessage })
    } catch (error) { setFeedback({ error: errorMessage(error) }) }
  }
  return <form className="validated-form" onSubmit={handleSubmit(submit)} noValidate aria-busy={isSubmitting}>
    <fieldset disabled={isSubmitting} className="login-fields">
      {fields.map((field) => {
        const fieldId = `${id}-${field.name}`
        const common = { ...register(field.name), id: fieldId, autoComplete: field.autoComplete, maxLength: field.maxLength, placeholder: field.placeholder, 'aria-invalid': !!errors[field.name], 'aria-required': field.required !== false, 'aria-describedby': [errors[field.name] ? `${fieldId}-error` : '', field.helper ? `${fieldId}-help` : ''].filter(Boolean).join(' ') || undefined }
        return <div className="form-field" key={field.name}><label htmlFor={fieldId}>{field.label}</label>
          {field.type === 'password' ? <PasswordInput {...common} /> : field.type === 'textarea' ? <textarea {...common} rows={4} /> : <Input {...common} type={field.type || 'text'} autoCapitalize="none" spellCheck={field.spellCheck || false} />}
          {field.helper && <p className="field-help" id={`${fieldId}-help`}>{field.helper}</p>}
          {errors[field.name] && <p className="field-error" id={`${fieldId}-error`} role="alert">{errors[field.name].message}</p>}
        </div>
      })}
      {children}
      <Button className="form-submit" type="submit" variant={destructive ? 'destructive' : 'default'} disabled={isSubmitting || submitDisabled}>{isSubmitting && <LoaderCircle className="spin" />}{isSubmitting ? t('common:saving') : submitLabel || t('common:save')}</Button>
    </fieldset>
    {feedback?.error && <p className="form-feedback feedback-error" role="alert">{feedback.error}</p>}
    {feedback?.success && <p className="form-feedback feedback-success" role="status">{feedback.success}</p>}
  </form>
}
