import { useTranslation } from "react-i18next";
import { useLayoutEffect, useRef, useState } from 'react';
import { LoaderCircle, Pencil, Reply, Send, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { errorMessage } from '@/lib/errors';
import { useAttachmentDraft } from '@/features/files/use-attachment-draft';
import { AttachmentDraft } from '@/features/files/AttachmentPreview';
export function Composer({
  onSend,
  disabled,
  preview,
  context,
  onCancelContext,
  inputRef,
  onTyping
}) {
  const {
    t
  } = useTranslation();
  const [draft, setDraft] = useState(context?.mode === 'edit' ? context.content : '');
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(null);
  const retryPayload = useRef(null);
  const field = useRef(null);
  const upload = useAttachmentDraft();
  useLayoutEffect(() => {
    const element = field.current;
    if (!element) return;
    element.style.height = 'auto';
    element.style.height = `${Math.min(element.scrollHeight, 144)}px`;
  }, [draft]);
  async function send(event) {
    event?.preventDefault();
    if ((!draft.trim() && !upload.readyIds.length) || upload.blocked || pending || disabled || draft.length > 10000) return;
    const content = draft.trim() || null;
    const attachmentIds = upload.readyIds;
    const signature = JSON.stringify({ content, attachmentIds, replyToMessageId: context?.mode === 'reply' ? context.id : null });
    const payload = retryPayload.current?.signature === signature ? retryPayload.current.payload : {
      content,
      clientMessageId: crypto.randomUUID(),
      ...(attachmentIds.length ? { attachmentIds } : {}),
      ...(context?.mode === 'reply' ? {
        replyToMessageId: context.id
      } : {})
    };
    retryPayload.current = { signature, payload };
    setPending(true);
    setError(null);
    try {
      await onSend(payload);
      onTyping?.(false);
      setDraft('');
      upload.clear();
      retryPayload.current = null;
      // readOnly while sending keeps keyboard/focus stable, including touch browsers.
      field.current?.focus({
        preventScroll: true
      });
    } catch (sendError) {
      setError(sendError);
    } finally {
      setPending(false);
    }
  }
  return <form className="composer" onSubmit={send} aria-label={t("chat:send-a-message")} aria-busy={pending}>
    {context && <div className="compose-context"><span aria-hidden="true">{context.mode === 'edit' ? <Pencil size={17} /> : <Reply size={17} />}</span><div><strong>{preview ? context.mode === 'edit' ? t("chat:editing-sample-message") : t("chat:replying-to-sample-message") : t(context.mode === 'edit' ? 'messages:editing' : 'messages:replying')}</strong><p>{context.content}</p></div><Button type="button" variant="ghost" size="icon" aria-label={t("chat:cancel-message-action")} onClick={onCancelContext}><X /></Button></div>}
    {error && <p className="inline-error" role="alert">{errorMessage(error)}{t("chat:your-draft-is-kept-try-sending-again")}</p>}
    {disabled && <p className="composer-permission" role="status">{t("chat:messaging-is-unavailable-your-conversation-access-may-have-changed")}</p>}
    {!preview && context?.mode !== 'edit' && !disabled && <AttachmentDraft upload={upload} disabled={pending} />}
    <div className="composer-field"><textarea ref={element => {
        field.current = element;
        if (inputRef) inputRef.current = element;
      }} autoFocus={!!context} aria-label={t("chat:message")} aria-describedby="composer-help" placeholder={disabled ? t("chat:messaging-is-unavailable") : context?.mode === 'edit' ? (preview ? t("chat:edit-your-sample-message") : t("messages:editPlaceholder")) : t("chat:write-a-little-something")} value={draft} disabled={disabled} readOnly={pending} rows={1} maxLength={10000} enterKeyHint="send" onChange={event => { setDraft(event.target.value); onTyping?.(!!event.target.value.trim()); }} onBlur={() => onTyping?.(false)} onKeyDown={event => {
        if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) send(event);
      }} />
      <Button type="submit" size="icon" className="send-button" aria-label={pending ? t("chat:sending-message") : context?.mode === 'edit' ? (preview ? t("chat:save-sample-edit") : t("messages:saveEdit")) : preview ? t("chat:send-sample-message") : t("chat:send-message")} disabled={disabled || pending || upload.blocked || (!draft.trim() && !upload.readyIds.length)}>{pending ? <LoaderCircle className="spin" /> : context?.mode === 'edit' ? <Pencil size={18} /> : <Send size={18} />}</Button>
    </div><div className="composer-hint" id="composer-help"><span>{preview ? t("chat:design-preview-changes-stay-in-this-page") : t("chat:enter-to-send-shift-enter-for-a-new-line")}</span><span>{draft.length > 9000 ? `${draft.length}/10000` : t("chat:a-little-closer-every-message")}</span></div>
  </form>;
}
