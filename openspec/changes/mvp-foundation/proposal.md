## Why

True Capture is a trust-first social platform whose differentiator is its admin-curated "Fake vs Real" awareness channel and (in later phases) AI-driven trust scoring. None of the consumer-facing surfaces exist yet — the backend has only `/api/auth/{register,login,refresh,logout}` and the Flutter app has only sign-in/sign-up screens that point at endpoint paths the backend doesn't expose. Without a working MVP, the editorial team cannot publish Fake-vs-Real reports and there is no feed for users to land on after login. This change delivers the Phase 1 MVP defined in `prd.md` — the smallest end-to-end slice that lets a real user sign up, see admin awareness posts, capture and post their own media, and engage with content. It explicitly defers trust infrastructure (Phase 2), AI detection (Phase 4), chat, push notifications, and search.

## What Changes

- Reconcile mobile/backend auth contracts: rename mobile endpoints to the existing `/api/auth/{register,login,refresh,logout}` (or add aliases) and add the missing OTP, forgot/reset-password, and Google OAuth flows.
- Add user profile, follow graph, public profile view, and profile-edit endpoints + screens.
- Add an in-app camera capture flow on mobile (photo, multi-photo, video) that embeds capture metadata (timestamp, device fingerprint, optional GPS) on every upload — the foundation that Phase 2 trust scoring will later read.
- Add a media upload + processing pipeline: signed upload URLs, image compression, video transcoding to HLS, thumbnail generation, background retry queue.
- Add the `posts` model with `is_admin_post` and `is_fake_vs_real` flags, hashtag + mention parsing, and clickable token rendering.
- Add the personalized Feed: `(followed-user posts) ∪ (admin posts)`, cursor-paginated, Redis-cached per user, with the cold-start rule that 0-follow users see admin-only posts (never an empty feed).
- Add the Fake vs Real tab as a filtered view of posts where `is_fake_vs_real = true`.
- Add engagement: like, comment (with one level of replies), share, save.
- Add server-driven CMS pages (About, ToS, Privacy, Community Guidelines) and a Contact Us form that creates backend tickets.
- Add app settings on mobile: theme (light/dark/system), notification preference storage, privacy toggles, logout.
- Split the web surface in two: keep `true_capture_web/` as the public SEO site (out of MVP scope after the split) and stand up a **new** `true_capture_admin_panel/` Next.js 15 app dedicated to the operational admin console.
- Add an **operational** admin panel under `true_capture_admin_panel/` covering: user moderation (ban / **restrict** / **mute** / **suspend** / verify); content moderation (hide / **remove** / **sensitive-veil**); Fake-vs-Real composer; **pre-publish approval queue** (policy-driven); **user analytics dashboard** (Postgres-view-backed); **broadcast / targeted announcements** (DB-polled by mobile until push lands); **admin-to-user email send**; **taxonomy / hashtag management** (banned + featured); CMS page editor; Contact Us inbox; **per-user data export**; **admin audit log**.
- Add backing schema and modules to support the expanded admin scope: `User.RestrictionLevel`, `User.RestrictionExpiresAtUtc`; `Post.Status` (`live | pending_review | rejected | removed`), `Post.Sensitive`; `admin_announcements`, `admin_audit_log`, `banned_hashtags`, `featured_hashtags`; `approval_policies`.
- Mobile: render `Post.Sensitive` posts with a tap-to-reveal blur overlay; poll `/api/me/announcements` to surface admin broadcasts in-app; respect `Post.Status` (only `live` posts in non-author feed reads); render a restriction banner on profile for the affected user.
- **BREAKING** (internal, pre-launch): mobile `ApiEndpoints` paths change from `/auth/sign-in` style to `/api/auth/login` style; existing local dev tokens will be invalidated.

Out of scope for this change (tracked for later phases): trust scoring, watermarking, device-fingerprint validation, AI/deepfake detection, NSFW auto-moderation, push notifications via FCM/APNS (replaced for MVP by DB-polled `admin_announcements`), 1-to-1 chat (SignalR), full search, trending algorithm, event-stream analytics infra (replaced for MVP by Postgres views), monetization, public SEO site at `true_capture_web/` (deferred — folder is reserved but not bootstrapped under this change).

**Scope note:** pulling analytics (originally Phase 5) and broadcast announcements (originally Phase 3) into MVP via lightweight DB-backed implementations grows the MVP burn estimate to **8–12 weeks** (vs. the PRD's original 6–8). Push delivery, AI review, and event-stream analytics remain in their later phases.

## Capabilities

### New Capabilities
- `user-auth`: Email + password signup/signin, email OTP verification, forgot/reset password, Google OAuth, JWT access + refresh-token rotation, rate-limited + captcha-gated anonymous endpoints. Supersedes the partial Identity module by completing the missing flows.
- `user-profile`: Username, display name, avatar, bio, public profile view, edit profile, follow/unfollow, followers/following counts and lists, admin-granted verification badge field (unused in UI until Phase 2).
- `media-capture-upload`: In-app camera (photo, multi-photo, video) with embedded capture metadata; signed-URL upload to S3/R2; background queue with retry; image compression; HLS video transcoding; thumbnail generation; persisted media records linked to posts.
- `posts`: Post entity supporting single photo, multi-photo carousel, and video; `is_admin_post` and `is_fake_vs_real` flags; caption with live `#hashtag` and `@mention` parsing; hashtag and mention index tables; create/read/delete endpoints.
- `feed`: Personalized feed assembly returning `(followed-user posts) ∪ (admin posts)`, ranked by recency + engagement; cursor-based pagination; per-user Redis cache with bounded invalidation; cold-start path that returns admin-only posts for 0-follow users; Fake-vs-Real filtered view over the same data.
- `engagement`: Like (toggle), comment with one level of replies, share (records share event + returns share URL), save/unsave; counters denormalized on the post.
- `cms-and-contact`: Server-driven CMS pages (About, Terms of Service, Privacy Policy, Community Guidelines) rendered from `cms_pages`; Contact Us form that writes to `contact_messages`.
- `app-settings`: Theme (light / dark / system) persisted locally and synced to server; notification-preference flags stored server-side (no delivery yet); privacy toggles; logout endpoint integration.
- `admin-panel`: Operational admin console hosted under `true_capture_admin_panel/` (Next.js 15, separate from the public web surface). Covers: user moderation (ban / unban / restrict / mute / suspend / verify); content moderation (hide / remove / sensitive-veil / Fake-vs-Real composer); pre-publish approval queue (policy-gated `Post.Status=pending_review`); user analytics dashboard (Postgres-view-backed — DAU/WAU, signups, posts, top creators, FvR reach); broadcast and targeted announcements (`admin_announcements`, polled by mobile); admin-to-user email send (reuses `IEmailSender`); taxonomy and hashtag management; CMS page editor; Contact Us inbox; per-user data export; append-only `admin_audit_log` written on every admin mutation. Restricted to users with `IsAdmin = true`.

### Modified Capabilities
- (none — no specs exist yet)

## Impact

- **Backend** (`true_capture_backend/`): new modules — `User`, `Media`, `Posts`, `Feed`, `Engagement`, `Cms`, `Admin`; extensions to existing `Identity` module for OTP, password reset, and Google OAuth; new EF migrations for `users` extensions, `follows`, `posts`, `post_media`, `post_hashtags`, `post_mentions`, `comments`, `likes`, `saves`, `shares`, `cms_pages`, `contact_messages`, `media_assets`; Redis dependency wired into composition root.
- **Mobile** (`true_capture_app/`): rename auth endpoints in `core/constants/api_endpoints.dart`; wire `AuthMixin.signInWithSocial` to the Google flow; add `presentation/screens/{main,feed,fake_vs_real,create,profile}` screens; in-app camera + composer; post card primitives; theme provider; CMS page renderer; settings screens.
- **Admin panel** (`true_capture_admin_panel/` — NEW folder): bootstrap Next.js 15 (App Router, TypeScript, server components default); HTTP-only cookie auth against `/api/auth/login`; middleware that 403s non-admins; sidebar layout; pages for Users, Posts, Approval Queue, Fake vs Real, Analytics, Announcements, Email, Taxonomy, CMS, Contact, Audit Log.
- **Public web** (`true_capture_web/`): folder retained but **not bootstrapped under this change** — SEO landing and public post pages are deferred. Will be addressed in a future OpenSpec change.
- **Infrastructure**: S3 / Cloudflare R2 bucket with signed URLs; Redis instance; HLS transcoding worker (FFmpeg-based); Cloudflare CDN in front of media; SMTP provider for OTP + password reset email.
- **External dependencies**: Google OAuth client credentials; captcha provider (already wired via `[RequireCaptcha]`); email provider; object storage credentials.
- **Flow docs**: `mobile_app_flow.md`, `backend_api_flow.md`, and `admin_panel_flow.md` must be updated for every module landed under this change. `web_app_flow.md` remains empty (public web deferred). The `.claude/hooks/flow_doc_reminder.sh` PostToolUse hook routes admin-panel file changes to `admin_panel_flow.md`.
- **Performance budgets**: feed load < 2 s (cached), upload start < 1 s, app cold start < 3 s — must be measured before this change is marked complete.
