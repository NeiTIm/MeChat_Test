import { useTranslation } from "react-i18next";
import { DropdownMenu } from 'radix-ui';
import { Heart, MoreHorizontal, Pencil, Reply, Undo2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
export function MessagePreviewActions({
  message,
  own,
  onAction
}) {
  const {
    t
  } = useTranslation();
  if (message.recalled) return null;
  return <DropdownMenu.Root><DropdownMenu.Trigger asChild><Button variant="ghost" size="icon" className="message-menu-trigger" aria-label={t("design:sample-message-value0-options", {
        value0: message.sequence
      })}><MoreHorizontal size={17} /></Button></DropdownMenu.Trigger>
    <DropdownMenu.Portal><DropdownMenu.Content className="message-menu" align={own ? 'end' : 'start'} sideOffset={5} collisionPadding={12}>
      <DropdownMenu.Label className="menu-caption">{t("design:design-preview-local-only")}</DropdownMenu.Label>
      <DropdownMenu.Item onSelect={() => onAction('reply', message)}><Reply />{t("design:reply")}</DropdownMenu.Item>
      <DropdownMenu.Item onSelect={() => onAction('react', message)}><Heart /> {message.previewReaction ? t("design:remove-heart-reaction") : t("design:react-with-heart")}</DropdownMenu.Item>
      {own && message.previewCanEdit && <DropdownMenu.Item onSelect={() => onAction('edit', message)}><Pencil />{t("design:edit-sample-message")}</DropdownMenu.Item>}
      {own && <DropdownMenu.Item className="menu-danger" onSelect={() => onAction('recall', message)}><Undo2 />{t("design:recall-sample-message")}</DropdownMenu.Item>}
    </DropdownMenu.Content></DropdownMenu.Portal>
  </DropdownMenu.Root>;
}
