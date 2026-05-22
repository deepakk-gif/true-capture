## 1. Infrastructure & Tooling

- [ ] 1.1 Provision PostgreSQL, Redis, and object storage (R2 bucket + Cloudflare CDN) for dev and staging; commit connection strings to `appsettings.Development.json` and document in `true_capture_backend/README.md`
- [ ] 1.2 Add `StackExchange.Redis` and Hangfire packages to `TrueCapture.Api`; wire Redis multiplexer and Hangfire in-process server in `Program.cs`
- [~] 1.3 Add a Roslyn analyzer or ArchUnit-style test that fails when one `TrueCapture.Modules.*` project references another module's entity types directly (enforces module boundaries — D1)  *(partial: `TrueCapture.Tests.Arch/ArchitectureTests.cs` has the NetArchTest boundary + BaseController + enum-status rules, but `ModuleAssemblies` only lists `Modules.Identity` — extend it to Users/Social/Notifications)*
- [ ] 1.4 Add Cloudflare Turnstile keys + captcha validator implementation behind the existing `[RequireCaptcha]` attribute (resolves OQ5; confirm with devops before merging)
- [x] 1.5 Configure SMTP provider (transactional email) for OTP delivery; add `EmailOptions` + `IEmailSender` interface and a SES/Mailgun-backed implementation
- [x] 1.6 Bootstrap Next.js 15 admin app under `true_capture_admin_panel/` (App Router, TypeScript, server components default); commit a stub layout and a `/login` page
- [x] 1.6b Leave `true_capture_web/` as a `.gitkeep`-only folder — the public SEO site is **not** built under this change; it will be addressed in a future OpenSpec change

## 2. Auth — Backend Extensions

- [x] 2.1 Add `OtpCode` entity (`UserId?`, `Email`, `CodeHash`, `Purpose: 'verify_email'|'password_reset'`, `ExpiresAtUtc`, `UsedAtUtc?`) + EF model config + migration
- [x] 2.2 Implement `IOtpService` with `GenerateAsync(email, purpose)`, `VerifyAsync(email, code, purpose)`; enforce 6-digit codes, 10-minute expiry, max 5 sends/hour per email
- [x] 2.3 Add `POST /api/auth/send-otp` and `POST /api/auth/verify-otp` to `AuthController`; captcha + rate-limit gated; update `backend_api_flow.md`
- [x] 2.4 Add `POST /api/auth/forgot-password` (sends OTP) and `POST /api/auth/reset-password` (consumes OTP, updates `PasswordHash`, revokes all active `RefreshToken`s for the user)
- [x] 2.5 Add `POST /api/auth/google` accepting a Google ID token; validate signature + audience via `Google.Apis.Auth`; create-or-match user by `GoogleSubject`; return `AuthTokensDto`
- [~] 2.6 Write integration tests covering every scenario in `specs/user-auth/spec.md`  *(reconciled 2026-05-21: unit tests exist — `AuthServiceTests`, `AuthServiceExtensionsTests`, `OtpServiceTests` — but they were committed not compiling, now fixed; no end-to-end integration tests for the auth spec yet)*

## 3. Auth — Mobile Reconciliation

- [x] 3.1 Update `lib/core/constants/api_endpoints.dart` to canonical paths (`/api/auth/{register,login,refresh,logout,send-otp,verify-otp,forgot-password,reset-password,google}`); delete the legacy `sign-in`/`sign-up`/`refresh-token`/`sign-out` constants
- [x] 3.2 Update `AuthRepository`, request DTOs, and OTP/forgot-password screens to use the new endpoint names; verify the flow on Android + iOS simulators
- [x] 3.3 Wire `AuthMixin.signInWithSocial` for Google: integrate `google_sign_in` plugin, retrieve ID token, call `AuthRepository.googleSignIn`, persist tokens via `AuthStateNotifier.saveToken`
- [x] 3.4 Implement the refresh-token interceptor in `lib/network/interceptors/` that retries `401` once after calling `/api/auth/refresh` and replaying the original request
- [x] 3.5 Add a Reset Password screen consuming `/api/auth/reset-password` (currently the OTP screen auto-logs in — split flows by `purpose`)
- [x] 3.6 Update `mobile_app_flow.md` Auth Module section: new endpoint paths, Google flow, refresh interceptor, reset-password screen

## 4. User Profile

- [x] 4.1 Create `TrueCapture.Modules.Users` module project; add `AddUsersModule` DI extension; register in `Program.cs`
- [x] 4.2 Add `Follow` entity — shipped as `(FollowerId, FolloweeId, Status)` with a `FollowStatus` enum (`Accepted`/`Pending`) for the public/private follow-request flow; unique index on the `(FollowerId, FolloweeId)` pair; migration `20260521112647_AddSocialAndNotices` *(self-follow blocked in `SocialService`, not a DB CHECK)*
- [x] 4.3 Profile + follow endpoints implemented across `UsersController` (self: `GET /api/users/me`, `PUT /api/users/me`, avatar) and `SocialController` (`GET /api/users/{id}`, `POST/DELETE /api/users/{id}/follow`, `GET /api/users/{id}/{followers|following}`) — **id-based routing, `PUT` not `PATCH`** (spec amended 2026-05-21)
- [x] 4.4 Add denormalized counters `FollowersCount`, `FollowingCount`, `PostsCount` on `User`; updated transactionally via `ExecuteUpdate` on follow-accept/unfollow/post-create/post-delete; migration `20260521142221_AddUserCounters` backfills existing rows
- [x] 4.5 Avatar upload — shipped as direct multipart upload (`POST/DELETE /api/users/me/avatar` → `IFileStorage`); the media-capture-upload signed-URL flow is **not** built under this change so avatars do not use it (spec/task amended 2026-05-21)
- [x] 4.6 Flutter: build the Main shell scaffold (`main_screen.dart`) with bottom tabs and persistent top app bar; left avatar opens own profile, right chat icon routes to "Coming soon" placeholder
- [x] 4.7 Flutter: implement Profile tab (`tab_profile.dart` — header, posts grid, settings entry points)
- [x] 4.8 Flutter: public-profile screen (`social/profile/user_profile_screen.dart`) reused for `@mention`/avatar taps — shows `followState` and a follow/unfollow button; followers/following lists via `social/follow/follow_list_screen.dart`
- [~] 4.9 Integration tests for every scenario in `specs/user-profile/spec.md` — written (`TrueCapture.Tests.Integration/UserProfileTests.cs`, compiles); **unverified — `WebAppFixture` needs Docker (Testcontainers PostgreSQL), unavailable in this environment**

## 5. Media Capture & Upload

- [x] 5.1 `MediaAsset` entity added in `Modules.Social` (`OwnerId`, `Kind`, `Status`, `StorageKey`, `Url`, `ThumbnailUrl?`, `MimeType`, `ByteSize`, `DurationSeconds?`, `Width?`/`Height?`, `CaptureMetadata` jsonb, `ErrorCode?`) + migration `20260522060437_AddCreatePostModule` *(consolidated into `Modules.Social`, not a separate `Modules.Media`)*
- [x] 5.2 `POST /api/media/uploads` (`MediaController` → `MediaService.RequestUploadAsync`) — MIME allow-list (jpeg/png/webp, mp4/quicktime; GIF/audio rejected), 25 MB/200 MB caps → `413`, 15-min ticket. *(Local dev uses an authorized `PUT /api/media/blob/{id}` endpoint via `IFileStorage.ReserveSlot`/`WriteAsync`; true S3/R2 presigning swaps in behind the same interface.)*
- [x] 5.3 `POST /api/media/finalize` (`MediaService.FinalizeAsync`) verifies bytes and flips the asset to `ready`, storing `capture_metadata` verbatim
- [ ] 5.4 Background FFmpeg/HLS transcoding worker — **deferred**; MVP serves photo bytes as their own thumbnail and stores video as-is (state machine `pending|ready|failed` is in place for the worker to be added later)
- [ ] 5.5 Enforce video duration cap server-side (probe with `ffprobe` before transcoding); set `status=failed` with `error_code=duration_exceeded`
- [ ] 5.6 Flutter: add `camera` plugin integration; build the Create tab camera surface (photo/video modes, front/back, flash, zoom); **no gallery picker**
- [ ] 5.7 Flutter: assemble `capture_metadata` payload on every capture (timestamp, device fingerprint from `device_info_plus`, install UUID, optional GPS from `geolocator`, `in_app_capture=true`)
- [ ] 5.8 Flutter: implement upload queue with retry (exponential backoff 1s/4s/16s, abort after) using a background isolate; persist queue state so app kill/restart resumes
- [ ] 5.9 Integration tests for every scenario in `specs/media-capture-upload/spec.md`

## 6. Posts

> **Module-consolidation note (2026-05-21):** Posts, Engagement, and Feed (sections 6/7/8) ship inside one `TrueCapture.Modules.Social` project, not three separate `Modules.Posts` / `Modules.Engagement` / `Modules.Feed` projects — see the D1 MVP amendment in `design.md`. Read "create the X module" tasks below as "add the X namespace/entities/services inside `Modules.Social`".

- [x] 6.1 `Post` rewritten in `Modules.Social` (`Type {Normal,FakeVsReal}`, `Kind`, `IsAdminPost`, `Status {Live,PendingReview,Removed}`, `RemovalReason?`, `ShareId`, counters `View/Likes/Comments/Shares/True/FalseVotes`); added `PostMedia`, `PostReference`, `PostMention` + 6 more edge tables. *(`Type` enum replaces `IsFakeVsReal`; `#hashtags` deferred — see 6.4.)*
- [x] 6.2 `is_fake_vs_real ⇒ is_admin_post` invariant enforced in `PostService` (Fake-vs-Real needs `CanPostFakeVsReal`; admin path sets `IsAdminPost`). *(`Verdict` enum dropped — voting replaces a fixed verdict.)*
- [x] 6.3 `POST /api/posts` + `POST /api/admin/posts` (`PostService.Create/AdminCreateAsync`) — verifies media `ready` + owned, derives `Kind`, rejects mixed media, requires caption + ≥1 reference for Fake-vs-Real
- [x] 6.4 Caption mention parsing in `PostService.ResolveAndNotifyMentionsAsync` — resolves `@username`, persists `PostMention` + notifies only where the target is public OR followed by the author. *(`#hashtag` indexing deferred to search.)*
- [x] 6.5 `GET /api/posts/{id}` (`EngagementService.GetPostAsync`), `GET /api/admin/users/{id}/posts`, `DELETE /api/posts/{id}` (author or admin) — id-based routing
- [~] 6.6 Flutter: build the post-card widget rendering all primitives in `specs/posts/spec.md` (avatar, media, carousel indicator, video auto-play, action row, save, caption with clickable tokens, relative time)  *(partial: `post_detail_screen.dart` exists; no reusable feed post-card, no carousel/video/clickable-token rendering)*
- [ ] 6.7 Flutter: implement the relative-time formatter exactly per spec (`Just now` / `Nm` / `Nh` / `Nd` / `Nw` / `Ny`); cover with unit tests
- [~] 6.8 Flutter: implement Create-tab composer screen (caption input with live `#`/`@` token highlight; submits to `POST /api/posts` after upload finalizes)  *(partial: `social/post/create_post_screen.dart` does image + caption upload; no live `#`/`@` token highlight)*
- [ ] 6.9 Flutter: hashtag results screen (route `/h/{tag}`) — placeholder using `GET /api/posts?hashtag=...` if exposed, or "Coming soon" stub if search is deferred to Phase 3
- [ ] 6.10 Integration tests for every scenario in `specs/posts/spec.md`

## 7. Feed

- [x] 7.1 `IFeedService` / `FeedService` in `Modules.Social` — Home channel: Normal + Live + (admin OR self OR followed OR public author); Fake-vs-Real channel: all Live Fake-vs-Real posts
- [ ] 7.2 Engagement-decay ranking — **deferred**; MVP orders newest-first (keyset on descending id)
- [x] 7.3 `GET /api/feed?channel=&cursor=` controller (`FeedController`); `channel=fake_vs_real` filters to Fake-vs-Real posts
- [ ] 7.4 Implement Redis cache layer: key `feed:{user_id}:{channel}` stores first N pages; TTL 5 min; invalidate on follow/unfollow (own key) and on new admin post (broadcast `feed:*` delete-by-pattern); maintain `followers:{author_id}` set for invalidation on new author posts
- [ ] 7.5 Verify cold-start behavior: a user with 0 follows receives admin-only posts (covered by query naturally; add integration test)
- [ ] 7.6 Flutter: implement Feed tab and Fake-vs-Real tab as `BaseConsumerState` screens hitting the same `FeedRepository.getFeed(channel)` method with pull-to-refresh and cursor-based infinite scroll
- [ ] 7.7 Flutter: feed cache reset on follow/unfollow (clear local cached page 1 to match server invalidation)
- [ ] 7.8 Integration tests for every scenario in `specs/feed/spec.md`; load test the cached path against the < 2 s target from PRD §11

## 8. Engagement

- [x] 8.1 Engagement entities in `Modules.Social`: `PostLike`, `Comment` (now with `ParentCommentId?` + `IsRemoved`), `PostSave`, `PostShare`, plus `PostVote`, `PostView`, `CommentLike` + migration
- [x] 8.2 `POST /api/posts/{id}/like|save|share` (toggles + canonical URL), `POST /api/posts/{id}/vote`, `POST/GET /api/posts/{id}/comments`, `GET /api/comments/{id}/replies`, `POST /api/comments/{id}/like`, `DELETE /api/comments/{id}`
- [x] 8.3 One-level reply rule enforced in `EngagementService.AddCommentAsync` — a `parentCommentId` whose own `ParentCommentId` is set is rejected
- [x] 8.4 Post counters (`LikesCount`/`CommentsCount`/`SharesCount`/`True`/`FalseVotesCount`) are denormalized columns updated in the same transaction as each engagement write
- [ ] 8.5 `POST /api/admin/maintenance/reconcile-counters` — deferred
- [x] 8.6 `GET /api/users/me/saves` cursor-paginated (`EngagementService.GetSavedAsync`)
- [ ] 8.7 Flutter: wire post-card action row (like toggle, comment sheet, share intent, save toggle); optimistic UI with rollback on failure
- [~] 8.8 Flutter: comment screen — list top-level comments with reply chevrons; fetch replies on tap; compose form supports `parent_comment_id`  *(partial: `social/post/comments_screen.dart` lists + composes top-level comments; no reply thread)*
- [ ] 8.9 Integration tests for every scenario in `specs/engagement/spec.md`

## 9. CMS & Contact

- [ ] 9.1 Create `TrueCapture.Modules.Cms` module; add `CmsPage` (`Slug` PK, `Title`, `BodyMarkdown`, `UpdatedAtUtc`) and `ContactMessage` (`Id`, `UserId?`, `Email`, `Subject`, `Body`, `Status`, `CreatedAtUtc`) + migrations
- [ ] 9.2 Seed `cms_pages` with `about`, `terms-of-service`, `privacy-policy`, `community-guidelines` rows (titles populated, bodies empty)
- [ ] 9.3 Implement `GET /api/cms/pages/{slug}` (anonymous, `Cache-Control: public, max-age=300`) and `PUT /api/admin/cms/pages/{slug}` (admin only)
- [ ] 9.4 Implement `POST /api/contact` (rate-limited per IP, captcha for anonymous), `GET /api/admin/contact?status=`, `PATCH /api/admin/contact/{id}`
- [ ] 9.5 Flutter: build CMS page renderer screen consuming `flutter_markdown` and `/api/cms/pages/{slug}`; link from Profile settings to each of the four slugs
- [ ] 9.6 Flutter: build Contact Us form under Profile settings posting to `/api/contact`; show success toast
- [ ] 9.7 Integration tests for every scenario in `specs/cms-and-contact/spec.md`

## 10. App Settings (Mobile + Server Mirror)

- [ ] 10.1 Add `UserSettings` (`UserId` PK, `Theme: light|dark|system`, `NotificationPrefs JSONB`, `PrivacyPrefs JSONB`, `UpdatedAtUtc`) + migration; seed default row on user creation
- [ ] 10.2 Implement `GET /api/users/me/settings` and `PATCH /api/users/me/settings`
- [ ] 10.3 Enforce `privacy_prefs.allow_mentions=false` in the post-create mention parser (skip rows; preserve caption text)
- [~] 10.4 Flutter: theme provider reads `SharedPreferences` synchronously before `runApp` to avoid theme flash; updates persist locally and PATCH the server (best-effort, no UI block on failure)  *(partial: local theme provider + `SharedPreferences` persistence exist; no server PATCH — `/api/users/me/settings` not built)*
- [ ] 10.5 Flutter: on first sign-in on a fresh device, fetch `/api/users/me/settings` and apply server theme value
- [ ] 10.6 Flutter: build Settings screens — Account (email, password, linked Google), Theme picker, Notification preferences (toggles, no delivery yet), Privacy
- [x] 10.7 Flutter: Logout entry — calls `/api/auth/logout`, clears tokens + cached user (preserves theme), routes to `routeSignIn`  *(implemented in `tab_profile.dart`)*
- [ ] 10.8 Integration tests for every scenario in `specs/app-settings/spec.md`

## 11. Admin Panel (Next.js 15 — `true_capture_admin_panel/`)

### 11a. Foundation
- [x] 11.1 Set up `true_capture_admin_panel/` with auth: login page → `/api/auth/login` → store tokens in HTTP-only cookies (`tc_access`, `tc_refresh`); `middleware.ts` redirects unauthenticated callers to `/login` and 403s non-admins
- [x] 11.2 Build dashboard layout (sidebar nav: Users, Posts, Approval Queue, Fake vs Real, Analytics, Announcements, Email, Taxonomy, CMS, Contact, Audit Log, Logout) and shared `DataTable` primitive at `components/data-table.tsx`
- [~] 11.3 Backend: implement every `/api/admin/*` endpoint guarded by an `[AdminOnly]` filter that requires `User.IsAdmin = true`; emit `admin_audit_log` rows transactionally on every mutating endpoint  *(partial: `[AdminOnly]` filter + policy provider landed; `GET /api/admin/users` covered; audit-log table + writes still TODO)*

### 11b. User and content moderation
- [ ] 11.4 Backend: add `User.RestrictionLevel` enum (`none|restricted|muted|suspended`) + `User.RestrictionExpiresAtUtc`; migration; revoke active refresh tokens when restriction transitions to `suspended` or `IsActive=false`
- [ ] 11.5 Backend: enforce restriction in middleware — `restricted` users get `403` on `POST /api/posts` and `POST /api/posts/{id}/comments`; `suspended` users blocked at `/api/auth/login`; `muted` users' posts/comments excluded from non-follower reads
- [~] 11.6 Admin Users page: searchable table + actions Ban / Unban / Restrict / Mute / Suspend (with expiry picker) / Clear / Verify / Revoke-Verify; PATCH `/api/admin/users/{id}`  *(partial: list page with search + tri-state filters + cursor pagination shipped; row-level mutation actions TODO with 11.4)*
- [ ] 11.7 Backend: add `Post.Status` enum (`live|pending_review|rejected|removed`), `Post.Sensitive`, `Post.RemovalReason`; migration; default `live`
- [ ] 11.8 Admin Posts page: review queue with filters; actions Hide / Remove (with reason) / Mark-Sensitive / Unmark / Delete

### 11c. Pre-publish approval queue
- [ ] 11.9 Backend: add `approval_policies(Id, Name, Rule, Active)` table; seed default policies (`author_age_days_lt=7`, `caption_contains_banned_hashtag`); migration
- [ ] 11.10 Backend: in post-create service, evaluate policies AFTER caption parsing — if any active policy matches, insert with `Status='pending_review'` instead of `live`
- [ ] 11.11 Backend: `PATCH /api/admin/posts/{id}/approval { decision: approve|reject, reason? }` transitions status and writes audit
- [ ] 11.12 Admin Approval Queue page: paginated list of `pending_review` posts with Approve / Reject actions
- [ ] 11.13 Flutter: render "Your post is pending review" indicator on the author's own profile when `Status='pending_review'`

### 11d. Fake-vs-Real composer
- [ ] 11.14 Fake-vs-Real composer: caption + up to 10 media (uses the same signed-URL pipeline as mobile) + verdict picker; submits to `POST /api/admin/posts`

### 11e. Analytics
- [ ] 11.15 Backend: create Postgres views `vw_dau`, `vw_wau`, `vw_retention_7d`, `vw_signups_daily`, `vw_posts_daily`, `vw_uploads_daily`, `vw_top_creators_7d`, `vw_fvr_engagement`; ship as raw SQL in a migration
- [ ] 11.16 Backend: implement `GET /api/admin/analytics/{view}?from=&to=&limit=` reading from those views
- [ ] 11.17 Admin Analytics page: charts for each view (recharts or equivalent); date-range picker; CSV export

### 11f. Broadcast / targeted announcements
- [ ] 11.18 Backend: add `admin_announcements` + `announcement_reads` tables; migration
- [ ] 11.19 Backend: `POST /api/admin/announcements` (admin) and `GET /api/me/announcements?cursor=&limit=` + `POST /api/me/announcements/{id}/read` (any authenticated user)
- [ ] 11.20 Admin Announcements composer: title + body + optional CTA URL + audience picker (`all_users`, `active_7d`, `active_30d`, `never_active`, `single_user`); expiry optional
- [ ] 11.21 Flutter: poll `/api/me/announcements` on app foreground + every 5 minutes; surface unread announcements in a banner above the feed; allow mark-as-read

### 11g. Admin-to-user email
- [ ] 11.22 Backend: add `admin_email_dispatches(Id, AdminId, RecipientUserId, Subject, BodyHash, SentAtUtc, Status)` table; migration
- [ ] 11.23 Backend: `POST /api/admin/emails` accepting `{ subject, body, audience, targetUserId? }`; dispatches via `IEmailSender`; enforces per-admin rate limit (5 broadcasts/day, 50 single-user/day); writes one `admin_email_dispatches` row per recipient and one `admin_audit_log` row per send
- [ ] 11.24 Admin Email composer: subject + body + audience picker; HTML preview

### 11h. Taxonomy management
- [ ] 11.25 Backend: add `banned_hashtags` + `featured_hashtags` tables; migration; seed empty
- [ ] 11.26 Backend: hook into post-create — caption containing a banned hashtag triggers the `caption_contains_banned_hashtag` approval policy match
- [ ] 11.27 Admin Taxonomy page: add / remove banned hashtags with reason; add / remove featured hashtags with optional expiry

### 11i. Data exports
- [ ] 11.28 Backend: implement `POST /api/admin/users/{id}/export` — enqueues a Hangfire job that assembles user JSON (profile, posts, media URLs, comments, likes, saves, shares, follows, settings), uploads to object storage, returns short-lived signed URL
- [ ] 11.29 Admin Users page: add "Export" row action that triggers the export and surfaces the resulting signed URL when complete

### 11j. Audit log
- [ ] 11.30 Backend: add `admin_audit_log` table; migration; helper `IAdminAuditWriter.WriteAsync(action, targetType, targetId, payload)` invoked from every `/api/admin/*` mutation
- [ ] 11.31 Backend: `GET /api/admin/audit?cursor=&adminId=&action=&targetType=` cursor-paginated, filters optional
- [ ] 11.32 Admin Audit Log page: filterable table; payload inspector modal

### 11k. CMS + Contact (existing)
- [ ] 11.33 CMS editor: list of `cms_pages` → inline title + markdown body editor → `PUT /api/admin/cms/pages/{slug}`
- [ ] 11.34 Contact inbox: filterable list → status transitions via `PATCH /api/admin/contact/{id}`

### 11l. Verification + flow doc
- [ ] 11.35 Integration tests for every scenario in `specs/admin-panel/spec.md`
- [ ] 11.36 Create `admin_panel_flow.md` content (already scaffolded at repo root) documenting each admin route + corresponding backend endpoint as modules land
- [ ] 11.37 Confirm `web_app_flow.md` remains empty / unmodified — the public web app is deferred under this change

## 12. Cross-Cutting Hardening

- [~] 12.1 Add Serilog structured logging for: auth events, post-create, media-finalize, feed reads (cached vs uncached), admin actions  *(partial: Serilog console+file configured in `Program.cs`; no per-event structured logging yet)*
- [~] 12.2 Confirm rate-limit policies on every anonymous endpoint (`/auth/*`, `/contact`); add per-user rate limit on `/api/media/uploads` (max 20/hour)  *(partial: `RateLimitPolicies.Auth` on `/api/auth/*`, `RateLimitPolicies.Upload` on avatar/post uploads; `/contact` + `/api/media/uploads` not built)*
- [ ] 12.3 Verify signed-URL upload buckets are private (no public list/get); CDN delivery uses signed read URLs or public read with random keys
- [ ] 12.4 Add an Admin → Maintenance action that resets a user's password (escape hatch for OQ — broken email inbox)
- [ ] 12.5 Resolve open questions OQ1 (R2 vs S3), OQ3 (video length cap), OQ4 (comment depth = 1), OQ5 (Turnstile), OQ6 (manual badge), **OQ7 (sensitive-veil session vs device persistence), OQ8 (broadcast audience selectors), OQ9 (analytics retention)** with stakeholders; update specs/design only if decisions change

## 13. Definition of Done

- [ ] 13.1 All scenarios in every `specs/*/spec.md` are covered by passing integration tests
- [ ] 13.2 PRD §11 performance budgets measured locally: feed (cached) < 2 s, upload start < 1 s, app cold start < 3 s
- [ ] 13.3 `mobile_app_flow.md`, `backend_api_flow.md`, and `web_app_flow.md` updated for every module landed
- [ ] 13.4 Manual end-to-end smoke:
  - Register → verify OTP → open Feed (admin-seeded) → follow another test user
  - Capture and post a photo + a multi-photo + a video → like / comment / save / share
  - Admin publishes a Fake-vs-Real post → mobile sees it in Fake vs Real tab
  - Admin moderates a post (hide / remove) → user receives moderated state
  - Trigger an approval-policy match (post from < 7-day-old test account) → post lands in approval queue; admin approves one and rejects one with reason
  - Admin marks a post sensitive → mobile renders blur overlay; tap-to-reveal works; relaunch re-applies
  - Admin restricts a user → that user cannot post; banner shown on their profile
  - Admin sends a broadcast announcement → mobile poll surfaces it within 5 minutes; mark-as-read works
  - Admin sends a single-user email → recipient gets it via SMTP; `admin_email_dispatches` row exists
  - Admin bans a hashtag → new post containing it enters approval queue
  - Admin exports a user → signed URL returns valid JSON
  - Admin opens analytics dashboard → DAU/WAU/top-creators render against real data
  - Admin Audit Log → every action above appears as a row
- [ ] 13.5 `openspec status --change mvp-foundation` reports all artifacts done and apply-ready
