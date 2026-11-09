import { translate } from '@/i18n'
import { useTranslation } from 'react-i18next'
import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  ArrowRight,
  Eye,
  EyeOff,
  LoaderCircle,
  LockKeyhole,
  MessageCircle,
  Sparkles,
} from 'lucide-react'
import { Brand } from '@/components/Brand'
import { ThemeToggle } from '@/components/ThemeToggle'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { authApi } from './api'
import { errorMessage } from '@/lib/errors'
export const loginSchema = z.object({
  identifier: z.string().trim().min(1, translate('auth:requiredIdentifier')),
  password: z.string().min(1, translate('auth:requiredPassword')),
})
export function LoginPage({ preview = false, previewState = 'populated', secondaryActions }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [visible, setVisible] = useState(false)
  const [feedback, setFeedback] = useState(null)
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({
    resolver: zodResolver(loginSchema),
    defaultValues: {
      identifier: '',
      password: '',
    },
  })
  async function submit(values) {
    setFeedback(null)
    if (preview) {
      setFeedback({
        preview: true,
        text: t('auth:design-preview-validation-passed-no-sign-in-request-was-sent'),
      })
      return
    }
    try {
      await authApi.login(values)
      const returnTo = searchParams.get('returnTo')
      navigate(
        returnTo &&
          /^\/(?:chat|groups|message-requests)(?:\/|\?|$)/.test(returnTo) &&
          !returnTo.includes('\\')
          ? returnTo
          : '/chat',
        {
          replace: true,
        },
      )
    } catch (error) {
      setFeedback({
        text: errorMessage(error),
      })
    }
  }
  const pending = isSubmitting || (preview && previewState === 'loading')
  return (
    <main
      className={`login-page ${preview ? 'login-preview' : ''}`}
      id="main-content"
    >
      <section
        className="login-story"
        aria-label={t('auth:welcome-to-mechat')}
      >
        <Brand />
        <div className="story-main">
          <span className="eyebrow">
            <span className="tiny-line" />
            {t('auth:less-distance-more-connection')}
          </span>
          <h1>
            {t('auth:a-little-closer')}
            <br />
            <span>{t('auth:every-conversation')}</span>
          </h1>
          <p>
            {t('auth:a-space-for-your-people-your-ideas')}
            <br className="desktop-break" />
            {t('auth:and-all-the-little-things-in-between')}
          </p>
          <div
            className="conversation-art"
            aria-hidden="true"
          >
            <div className="art-orbit orbit-one" />
            <div className="art-orbit orbit-two" />
            <div className="art-bubble art-bubble-one">
              <span className="art-avatar">
                <MessageCircle size={24} />
              </span>
              <div>
                <span className="art-line line-long" />
                <span className="art-line line-short" />
              </div>
            </div>
            <div className="art-bubble art-bubble-two">
              <span className="art-line" />
              <span className="art-line line-short" />
              <span className="art-spark">
                <Sparkles size={20} />
              </span>
            </div>
            <div className="art-caption">
              <span className="caption-dot" />
              {t('auth:good-conversations-start-here')}
            </div>
          </div>
        </div>
        <div className="story-footer">
          <span className="mini-symbol">m.</span>
          <span>{t('auth:made-for-meaningful-connections')}</span>
          <span className="story-year">MECHAT / 2026</span>
        </div>
      </section>
      <section
        className="login-form-panel"
        aria-labelledby="login-heading"
      >
        <header className="login-topbar">
          <span className="mobile-brand">
            <Brand />
          </span>
          <ThemeToggle />
        </header>
        <div className="login-form-wrap">
          <span
            className="welcome-symbol"
            aria-hidden="true"
          >
            <MessageCircle size={25} />
          </span>
          <span className="eyebrow form-eyebrow">
            {t('auth:your-people-are-a-conversation-away')}
          </span>
          <h2 id="login-heading">
            {t('auth:welcome-back')}
            <span className="brand-period">.</span>
          </h2>
          <p className="login-intro">{t('auth:pick-up-right-where-you-left-off')}</p>
          <form
            onSubmit={handleSubmit(submit)}
            noValidate
            aria-busy={pending}
          >
            <fieldset
              disabled={pending}
              className="login-fields"
            >
              <div className="form-field">
                <label htmlFor="identifier">{t('auth:email-or-username')}</label>
                <Input
                  id="identifier"
                  autoComplete="username"
                  autoCapitalize="none"
                  spellCheck={false}
                  placeholder={t('auth:enter-your-email-or-username')}
                  aria-invalid={!!errors.identifier}
                  aria-describedby={errors.identifier ? 'identifier-error' : undefined}
                  {...register('identifier')}
                />
                {errors.identifier && (
                  <p
                    id="identifier-error"
                    className="field-error"
                    role="alert"
                  >
                    {errors.identifier.message}
                  </p>
                )}
              </div>
              <div className="form-field">
                <label htmlFor="password">{t('auth:password')}</label>
                <div className="password-wrap">
                  <Input
                    id="password"
                    type={visible ? 'text' : 'password'}
                    autoComplete="current-password"
                    placeholder={t('auth:enter-your-password')}
                    aria-invalid={!!errors.password}
                    aria-describedby={errors.password ? 'password-error' : undefined}
                    {...register('password')}
                  />
                  <Button
                    className="password-toggle"
                    type="button"
                    variant="ghost"
                    size="icon"
                    aria-label={visible ? t('auth:hide-password') : t('auth:show-password')}
                    onClick={() => setVisible(!visible)}
                  >
                    {visible ? <EyeOff /> : <Eye />}
                  </Button>
                </div>
                {errors.password && (
                  <p
                    id="password-error"
                    className="field-error"
                    role="alert"
                  >
                    {errors.password.message}
                  </p>
                )}
              </div>
            </fieldset>
            {preview && previewState === 'error' && (
              <p
                className="form-feedback feedback-error"
                role="alert"
              >
                {t(
                  'auth:design-preview-we-couldn-t-sign-you-in-check-your-credentials-and-try-again',
                )}
              </p>
            )}
            {preview && previewState === 'loading' && (
              <p
                className="sr-only"
                role="status"
              >
                {t('auth:design-preview-signing-in-please-wait')}
              </p>
            )}
            {feedback && (
              <p
                className={feedback.preview ? 'form-feedback' : 'form-feedback feedback-error'}
                role="alert"
              >
                {feedback.text}
              </p>
            )}
            <Button
              className="login-submit"
              type="submit"
              disabled={pending}
            >
              {pending ? (
                <>
                  <LoaderCircle className="spin" />
                  {t('auth:signing-you-in')}
                </>
              ) : (
                <>
                  {t('auth:sign-in')}
                  <ArrowRight />
                </>
              )}
            </Button>
          </form>
          {secondaryActions ||
            (!preview && (
              <div className="auth-secondary-actions">
                <Link to="/forgot-password">{t('auth:forgot-password')}</Link>
                <span>
                  {t('auth:new-to-mechat')}{' '}
                  <Link to="/register">{t('auth:create-an-account')}</Link>
                </span>
                <Link to="/reactivate">{t('auth:pages.reactivate.link')}</Link>
                <Link to="/appeals">{t('appeals:title')}</Link>
              </div>
            ))}
          <div className="login-reassurance">
            <LockKeyhole size={14} />
            <span>{t('auth:your-space-your-conversations')}</span>
          </div>
          {import.meta.env.DEV && !preview && (
            <Link
              className="design-entry"
              to="/design/private"
            >
              {t('auth:explore-the-design-preview')}
              <ArrowRight size={14} />
            </Link>
          )}
          {preview && (
            <p className="preview-form-note">{t('auth:design-preview-form-validation-only')}</p>
          )}
        </div>
        <footer className="login-footer">
          <span>{t('auth:thoughtfully-connected')}</span>
          <span>MeChat</span>
        </footer>
      </section>
    </main>
  )
}
