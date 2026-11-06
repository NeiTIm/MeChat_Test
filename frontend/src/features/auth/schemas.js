import { z } from 'zod'

export function authSchemas(t) {
  const required = z.string().trim().min(1, t('common:required'))
  const email = z.string().trim().min(1, t('common:required')).email(t('auth:validation.email'))
  const password = z.string().min(12, t('auth:validation.passwordLength'))
  const confirm = (schema, key) => schema.refine((values) => values[key] === values.confirmPassword, { message: t('auth:validation.passwordMismatch'), path: ['confirmPassword'] })
  return {
    register: confirm(z.object({ email, username: z.string().trim().min(3, t('auth:validation.usernameMin')).max(50, t('auth:validation.usernameMax')), password, confirmPassword: z.string() }), 'password'),
    verifyEmail: z.object({ token: required }), resendVerification: z.object({ email }), forgotPassword: z.object({ email }),
    resetPassword: confirm(z.object({ token: required, newPassword: password, confirmPassword: z.string() }), 'newPassword'),
    reactivate: z.object({ identifier: required, password: z.string().min(1, t('auth:requiredPassword')) }),
    changePassword: confirm(z.object({ currentPassword: z.string().min(1, t('auth:requiredPassword')), newPassword: password, confirmPassword: z.string() }), 'newPassword'),
    deactivate: z.object({ password: z.string().min(1, t('auth:requiredPassword')) }),
  }
}
