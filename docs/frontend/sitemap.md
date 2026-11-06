# Sitemap

Design gate routes implemented first: `/login`, `/chat`, `/chat/:id`, `/groups`, `/groups/:id`. Development-only `/design/login`, `/design/private`, `/design/group` show labeled fixtures for approval. Production build excludes fixture modules and review routes.

After approval, public Auth area: register, verify-email, resend-verification, forgot-password, reset-password, reactivate. Protected app: inbox, people/search, friends/requests/blocks, user profile, message requests, groups/invitations/link resolver, notifications, account/profile/privacy/password/sessions, reports/mine. Separate appeal authentication and appeal-only area. Admin area: dashboard, users, groups, reports/case/evidence, appeals, audit, retention preview/plan/hold.

Navigation is added as the corresponding slice is implemented. No clickable placeholder navigation. Desktop rail persists between inbox/groups. Mobile bottom navigation becomes hidden while a conversation is open; back returns to the list. Details panel is optional on desktop and a modal sheet on mobile.
