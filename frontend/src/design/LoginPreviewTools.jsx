import { useTranslation } from "react-i18next";
import { useState } from 'react';
import { ArrowRight, KeyRound, Mail, UserPlus } from 'lucide-react';
import { PreviewDialog } from './PreviewDialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
export function LoginPreviewTools() {
  const {
    t
  } = useTranslation();
  return <div className="auth-secondary-actions"><PreviewDialog title={t("auth:reset-your-password")} trigger={<Button type="button" variant="link">{t("auth:forgot-password")}</Button>}>
    <EmailPreview kind="reset" />
  </PreviewDialog><span>{t("auth:new-to-mechat")}<PreviewDialog title={t("auth:make-room-for-connection")} trigger={<Button type="button" variant="link">{t("auth:create-an-account")}<ArrowRight size={14} /></Button>}>
    <div className="auth-preview-intro"><UserPlus /><p>{t("auth:registration-will-ask-for-your-email-a-username-and-a-password-of-at-least-12-characters-email-verification-is-required-before-signing-in")}</p></div>
    <p className="prototype-explanation">{t("auth:this-review-covers-the-entry-point-and-its-dialog-the-complete-registration-screen-stays-behind-the-design-approval-gate")}</p>
  </PreviewDialog></span></div>;
}
function EmailPreview() {
  const {
    t
  } = useTranslation();
  const [sent, setSent] = useState(false);
  return sent ? <div className="preview-success" role="status"><Mail /><h3>{t("auth:check-your-inbox")}</h3><p>{t("auth:preview-of-the-confirmation-state-no-email-was-sent-the-live-flow-will-show-the-same-neutral-response-whether-or-not-an-account-exists")}</p></div> : <form className="preview-form" onSubmit={event => {
    event.preventDefault();
    setSent(true);
  }}>
    <p>{t("auth:enter-your-email-and-we-ll-help-you-find-your-way-back")}</p><label htmlFor="reset-preview-email">{t("auth:email-address")}</label><Input id="reset-preview-email" type="email" required autoComplete="email" placeholder={t("auth:emailPlaceholder")} />
    <Button type="submit"><KeyRound />{t("auth:preview-reset-instructions")}</Button>
  </form>;
}
