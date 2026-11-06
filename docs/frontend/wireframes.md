# Wireframes — recorded before detailed JSX

## Login, desktop
```text
┌────────────────────────────────────┬──────────────────────────────────────┐
│ MeChat wordmark                     │                            Theme     │
│                                    │                                      │
│ A little closer.                   │  Welcome back                        │
│ Every conversation.                │  Short supporting text               │
│                                    │  Identifier [_________________]      │
│ Designed conversation illustration │  Password   [______________ eye]     │
│ (decorative, no fake live statuses) │  [         Sign in →          ]      │
│                                    │  Feedback / validation               │
│ Identity statement                  │  Development design review link     │
└────────────────────────────────────┴──────────────────────────────────────┘
```
Mobile: wordmark + theme → small brand tagline → form; decorative panel hidden; no clipped vertical content.

## Private chat, desktop
```text
┌──────┬──────────────────────┬──────────────────────────────────┬─────────────┐
│ Logo │ Messages        sync │ Avatar / name     search details │ About       │
│ Chat │ Filter input         ├──────────────────────────────────┤ Profile     │
│ Group│ All / unread         │ Date divider                     │ Conversation│
│      │ Conversation rows    │ Incoming bubble                  │ information │
│      │ avatar/name/metadata │               Outgoing bubble    │             │
│ Theme│                      │                                  │             │
│ Exit │                      ├──────────────────────────────────┤             │
│ Me   │                      │ Text composer             send   │             │
└──────┴──────────────────────┴──────────────────────────────────┴─────────────┘
```
Group: same skeleton with group avatar/name, member summary, labeled role chips in details; neutral topic strip. No call/video buttons (backend has no call capability).

## Mobile chat
```text
List                         Selected conversation
┌───────────────────────┐    ┌───────────────────────┐
│ Messages         theme│    │ Back  Name    Details │
│ Filter                │    ├───────────────────────┤
│ Conversation rows     │    │ Scrollable messages   │
│                       │    │                       │
├───────────────────────┤    ├───────────────────────┤
│ Messages      Groups  │    │ Composer         Send │
└───────────────────────┘    └───────────────────────┘
```
Details use accessible Dialog on narrow screens with focus trap, Escape and return focus. Theme switch, search/filter, thread selection, details and composer are functional in the samples. Later actions are omitted until implemented. Review fixtures may simulate state changes but are labeled and development-only.
