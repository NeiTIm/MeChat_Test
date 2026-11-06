# Responsive rules

- 320–767px: single pane. List or selected thread, never both squeezed side by side. Mobile bottom nav on list only; selected thread gets back control. Touch targets ≥44px. Details opens as modal dialog.
- 768–1023px: 76px navigation + a full-width list OR thread. A selected thread hides the list and shows Back. This replaces the cramped baseline 400px thread at 768px. Details remains modal.
- 1024–1199px: 76px navigation + 292px list + fluid thread. Details remains modal.
- ≥1200px: 84px rail + 320px list + fluid thread. At ≥1280px Details opens inline: 84px rail + 300px list + fluid thread + 280px details. Escape/Close returns focus to its trigger.
- Chat shell height uses 100dvh; `min-height:0` on nested flex/grid panels; overflow scroll belongs to list/history. Composer stays in grid flow; safe-area insets on mobile bottom. Visual viewport/real software keyboard behavior needs device verification.
- Bubbles are bounded to 560px and leave room for avatar/message tools. Long words/URLs wrap. Message actions remain visible on touch layouts and appear on hover/focus on larger layouts. Names truncate visually while retaining full accessible text/title. Verify no horizontal overflow at 320px; root font scaling and real browser zoom are separate checks.
- Login collapses decorative panel below 900px; form remains naturally scrollable on short viewports.
- Themes share geometry; reduced-motion and focus do not shift layout. Required matrix: 320/390×844, 768×1024, 1366/1440×1000, in both themes. The review toolbar is 50px desktop / 88px mobile and state controls 52px; preview shell subtracts these exactly. Live shell remains 100dvh. Login scrolls naturally on short screens. Keyboard-sized viewport test uses 390×480 and does not replace physical device testing.
