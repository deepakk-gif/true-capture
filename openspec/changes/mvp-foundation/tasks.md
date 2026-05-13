## 1. Infrastructure & Tooling

- [ ] 1.1 Provision PostgreSQL, Redis, and object storage (R2 bucket + Cloudflare CDN) for dev and staging; commit connection strings to `appsettings.Development.json` and document in `true_capture_backend/README.md`
- [ ] 1.2 Add `StackExchange.Redis` and Hangfire packages to `TrueCapture.Api`; wire Redis multiplexer and Hangfire in-process server in `Program.cs`
- [ ] 1.3 Add a Roslyn analyzer or ArchUnit-style test that fails when one `TrueCapture.Modules.*` project references another module's entity types directly (enforces module boundaries — D1)
- [ ] 1.4 Add Cloudflare Turnstile keys + captcha validator implementation behind the existing `[RequireCaptcha]` attribute (resolves OQ5; confirm with devops before merging)
- [ ] 1.5 Configure SMTP provider (transactional email) for OTP delivery; add `EmailOptions` + `IEmailSender` interface and a SES/Mailgun-backed implementation
- [ ] 1.6 Bootstrap Next.js 15 app under `true_capture_web/` (App Router, TypeScript, server components default); commit a stub layout and a `/login` page

## 2. Auth — Backend Extensions

- [ ] 2.1 Add `OtpCode` entity (`UserId?`, `Email`, `CodeHash`, `Purpose: 'verify_email'|'password_reset'`, `ExpiresAtUtc`, `UsedAtUtc?`) + EF model config + migration
- [ ] 2.2 Implement `IOtpService` with `GenerateAsync(email, purpose)`, `VerifyAsync(email, code, purpose)`; enforce 6-digit codes, 10-minute expiry, max 5 sends/hour per email
- [ ] 2.3 Add `POST /api/auth/send-otp` and `POST /api/auth/verify-otp` to `AuthController`; captcha + rate-limit gated; update `backend_api_flow.md`
- [ ] 2.4 Add `POST /api/auth/forgot-password` (sends OTP) and `POST /api/auth/reset-password` (consumes OTP, updates `PasswordHash`, revokes all active `RefreshToken`s for the user)
- [ ] 2.5 Add `POST /api/auth/google` accepting a Google ID token; validate signature + audience via `Google.Apis.Auth`; create-or-match user by `GoogleSubject`; return `AuthTokensDto`
- [ ] 2.6 Write integration tests covering every scenario in `specs/user-auth/spec.md`

## 3. Auth — Mobile Reconciliation

- [ ] 3.1 Update `lib/core/constants/api_endpoints.dart` to canonical paths (`/api/auth/{register,login,refresh,logout,send-otp,verify-otp,forgot-password,reset-password,google}`); delete the legacy `sign-in`/`sign-up`/`refresh-token`/`sign-out` constants
- [ ] 3.2 Update `AuthRepository`, request DTOs, and OTP/forgot-password screens to use the new endpoint names; verify the flow on Android + iOS simulators
- [ ] 3.3 Wire `AuthMixin.signInWithSocial` for Google: integrate `google_sign_in` plugin, retrieve ID token, call `AuthRepository.googleSignIn`, persist tokens via `AuthStateNotifier.saveToken`
- [ ] 3.4 Implement the refresh-token interceptor in `lib/network/interceptors/` that retries `401` once after calling `/api/auth/refresh` and replaying the original request
- [ ] 3.5 Add a Reset Password screen consuming `/api/auth/reset-password` (currently the OTP screen auto-logs in — split flows by `purpose`)
- [ ] 3.6 Update `mobile_app_flow.md` Auth Module section: new endpoint paths, Google flow, refresh interceptor, reset-password screen

## 4. User Profile

- [ ] 4.1 Create `TrueCapture.Modules.Users` module project; add `AddUsersModule` DI extension; register in `Program.cs`
- [ ] 4.2 Add `Follow` entity (`FollowerId`, `FollowedId`, `CreatedAtUtc`) with unique index on the pair + CHECK that `FollowerId != FollowedId`; migration
- [ ] 4.3 Implement `UsersController` + `UsersService` with: `GET /api/users/me`, `GET /api/users/{username}`, `PATCH /api/users/me`, `POST/DELETE /api/users/{username}/follow`, `GET /api/users/{username}/{followers|following}`
- [ ] 4.4 Add denormalized counters `FollowersCount`, `FollowingCount`, `PostsCount` on `User`; update them transactionally on follow/unfollow/post-create/post-delete
- [ ] 4.5 Wire profile-edit avatar upload through the media-capture-upload signed-URL flow (referenced by `media_asset_id` on `User.AvatarMediaAssetId`)
- [ ] 4.6 Flutter: build the Main shell scaffold (`main_screen.dart`) with bottom tabs and persistent top app bar; left avatar opens own profile, right chat icon routes to "Coming soon" placeholder
- [ ] 4.7 Flutter: implement Profile tab (header, posts grid via `GET /api/users/{username}/posts`, settings entry points)
- [ ] 4.8 Flutter: implement public-profile screen reused for `@mention` and avatar taps; show `you_follow` and a follow/unfollow button
- [ ] 4.9 Integration tests for every scenario in `specs/user-profile/spec.md`

## 5. Media Capture & Upload

- [ ] 5.1 Create `TrueCapture.Modules.Media` module; add `MediaAsset` entity (`Id`, `OwnerId`, `Kind: photo|video`, `Status: pending|ready|failed`, `MimeType`, `StorageKey`, `MasterUrl?`, `ThumbnailUrl?`, `HlsPlaylistUrl?`, `DurationMs?`, `CaptureMetadata JSONB`, `ErrorCode?`, `CreatedAtUtc`) + migration
- [ ] 5.2 Implement `POST /api/media/uploads` returning a pre-signed PUT URL (R2/S3), enforce MIME allow-list, enforce size caps (25 MB photo / 200 MB video), short expiry (≤ 15 min)
- [ ] 5.3 Implement `POST /api/media/finalize` creating the `MediaAsset` row in `status=pending` with the supplied `capture_metadata` (JSONB validated for required keys: `captured_at`, `device.*`, `in_app_capture`)
- [ ] 5.4 Add Hangfire job `MediaProcessor.ProcessAsync(mediaAssetId)` that runs FFmpeg for video (HLS 240p/480p/720p, 60s max) and a compression pipeline for photos (WebP master + thumbnail), uploads outputs, updates the row to `ready` or `failed`
- [ ] 5.5 Enforce video duration cap server-side (probe with `ffprobe` before transcoding); set `status=failed` with `error_code=duration_exceeded`
- [ ] 5.6 Flutter: add `camera` plugin integration; build the Create tab camera surface (photo/video modes, front/back, flash, zoom); **no gallery picker**
- [ ] 5.7 Flutter: assemble `capture_metadata` payload on every capture (timestamp, device fingerprint from `device_info_plus`, install UUID, optional GPS from `geolocator`, `in_app_capture=true`)
- [ ] 5.8 Flutter: implement upload queue with retry (exponential backoff 1s/4s/16s, abort after) using a background isolate; persist queue state so app kill/restart resumes
- [ ] 5.9 Integration tests for every scenario in `specs/media-capture-upload/spec.md`

## 6. Posts

- [ ] 6.1 Create `TrueCapture.Modules.Posts` module; add `Post` (`Id`, `AuthorId`, `Caption`, `Kind: photo|carousel|video`, `IsAdminPost`, `IsFakeVsReal`, `Verdict?`, `Hidden`, `LikesCount`, `CommentsCount`, `SharesCount`, `CreatedAtUtc`), `PostMedia` (`PostId`, `MediaAssetId`, `Position`), `PostHashtag` (`PostId`, `Tag`), `PostMention` (`PostId`, `MentionedUserId`) + migrations
- [ ] 6.2 Add CHECK constraint enforcing `is_fake_vs_real => is_admin_post`; add `Verdict` nullable enum column (`real|fake|misleading`) usable only when `is_fake_vs_real=true`
- [ ] 6.3 Implement `POST /api/posts` (regular users) and `POST /api/admin/posts` (admin only — sets admin flags + verdict); reject mixed media; verify all referenced `MediaAsset.Status='ready' AND OwnerId == author`
- [ ] 6.4 Implement caption parsing service: regex extracts hashtags and mentions, lowercases, resolves mentions to user ids, writes `PostHashtag` and `PostMention` rows; skips mentions where target user has `privacy_prefs.allow_mentions=false`
- [ ] 6.5 Implement `GET /api/posts/{id}`, `GET /api/users/{username}/posts`, `DELETE /api/posts/{id}` (author or admin)
- [ ] 6.6 Flutter: build the post-card widget rendering all primitives in `specs/posts/spec.md` (avatar, media, carousel indicator, video auto-play, action row, save, caption with clickable tokens, relative time)
- [ ] 6.7 Flutter: implement the relative-time formatter exactly per spec (`Just now` / `Nm` / `Nh` / `Nd` / `Nw` / `Ny`); cover with unit tests
- [ ] 6.8 Flutter: implement Create-tab composer screen (caption input with live `#`/`@` token highlight; submits to `POST /api/posts` after upload finalizes)
- [ ] 6.9 Flutter: hashtag results screen (route `/h/{tag}`) — placeholder using `GET /api/posts?hashtag=...` if exposed, or "Coming soon" stub if search is deferred to Phase 3
- [ ] 6.10 Integration tests for every scenario in `specs/posts/spec.md`

## 7. Feed

- [ ] 7.1 Create `TrueCapture.Modules.Feed` module; implement `IFeedService.GetAsync(userId, cursor, limit, channel?)` returning ranked posts via SQL: `WHERE is_admin_post = true OR author_id IN (SELECT followed_id FROM follows WHERE follower_id = @me)` AND `hidden = false` AND post has no `pending` media
- [ ] 7.2 Implement ranking: `score = log(1 + likes_count + 2*comments_count + 3*shares_count) - hours_since_post * 0.05`; ORDER BY score DESC, post id DESC; cursor encodes `(score, post_id)`
- [ ] 7.3 Implement `GET /api/feed?cursor=&limit=&channel=` controller; `channel=fake_vs_real` adds `AND is_fake_vs_real = true` and orders by `created_at DESC`
- [ ] 7.4 Implement Redis cache layer: key `feed:{user_id}:{channel}` stores first N pages; TTL 5 min; invalidate on follow/unfollow (own key) and on new admin post (broadcast `feed:*` delete-by-pattern); maintain `followers:{author_id}` set for invalidation on new author posts
- [ ] 7.5 Verify cold-start behavior: a user with 0 follows receives admin-only posts (covered by query naturally; add integration test)
- [ ] 7.6 Flutter: implement Feed tab and Fake-vs-Real tab as `BaseConsumerState` screens hitting the same `FeedRepository.getFeed(channel)` method with pull-to-refresh and cursor-based infinite scroll
- [ ] 7.7 Flutter: feed cache reset on follow/unfollow (clear local cached page 1 to match server invalidation)
- [ ] 7.8 Integration tests for every scenario in `specs/feed/spec.md`; load test the cached path against the < 2 s target from PRD §11

## 8. Engagement

- [ ] 8.1 Create `TrueCapture.Modules.Engagement` module; add `Like` (`UserId`, `PostId`, unique), `Comment` (`Id`, `PostId`, `AuthorId`, `Body`, `ParentCommentId?`, `DeletedAtUtc?`, `CreatedAtUtc`), `Save` (`UserId`, `PostId`, unique), `Share` (`UserId`, `PostId`, `CreatedAtUtc`) entities + migrations
- [ ] 8.2 Implement `POST /api/posts/{id}/like` (toggle), `POST /api/posts/{id}/save` (toggle), `POST /api/posts/{id}/share` (returns canonical URL), `POST /api/posts/{id}/comments`, `GET /api/posts/{id}/comments`, `GET /api/comments/{id}/replies`, `DELETE /api/comments/{id}`
- [ ] 8.3 Enforce one-level reply rule: reject `parent_comment_id` whose own `ParentCommentId IS NOT NULL`
- [ ] 8.4 Update post counters atomically on each engagement write (inside the same transaction as the row insert/delete)
- [ ] 8.5 Implement `POST /api/admin/maintenance/reconcile-counters` recomputing `likes_count`, `comments_count`, `shares_count` from row counts (admin-only)
- [ ] 8.6 Implement `GET /api/users/me/saves` cursor-paginated
- [ ] 8.7 Flutter: wire post-card action row (like toggle, comment sheet, share intent, save toggle); optimistic UI with rollback on failure
- [ ] 8.8 Flutter: comment screen — list top-level comments with reply chevrons; fetch replies on tap; compose form supports `parent_comment_id`
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
- [ ] 10.4 Flutter: theme provider reads `SharedPreferences` synchronously before `runApp` to avoid theme flash; updates persist locally and PATCH the server (best-effort, no UI block on failure)
- [ ] 10.5 Flutter: on first sign-in on a fresh device, fetch `/api/users/me/settings` and apply server theme value
- [ ] 10.6 Flutter: build Settings screens — Account (email, password, linked Google), Theme picker, Notification preferences (toggles, no delivery yet), Privacy
- [ ] 10.7 Flutter: Logout entry — calls `/api/auth/logout`, clears tokens + cached user (preserves theme), routes to `routeSignIn`
- [ ] 10.8 Integration tests for every scenario in `specs/app-settings/spec.md`

## 11. Admin Panel (Next.js 15)

- [ ] 11.1 Set up `true_capture_web/` with auth: login page → `/api/auth/login` → store tokens in HTTP-only cookies; middleware redirects unauthenticated callers and 403s non-admins
- [ ] 11.2 Build dashboard layout (sidebar nav: Users, Posts, Fake vs Real, CMS, Contact, Logout) and shared data-table component
- [ ] 11.3 Users page: searchable table + Ban / Unban / Verify / Revoke-Verify actions calling `PATCH /api/admin/users/{id}`; bans revoke that user's active refresh tokens server-side
- [ ] 11.4 Posts page: review queue with filters (`is_admin_post`, `is_fake_vs_real`, author); Hide / Delete actions calling `PATCH /api/admin/posts/{id}` and `DELETE /api/posts/{id}`
- [ ] 11.5 Fake-vs-Real composer: caption + up to 10 media (uses the same signed-URL pipeline as mobile) + verdict picker; submits to `POST /api/admin/posts`
- [ ] 11.6 CMS editor: list of `cms_pages` → inline title + markdown body editor → `PUT /api/admin/cms/pages/{slug}`
- [ ] 11.7 Contact inbox: filterable list → status transitions via `PATCH /api/admin/contact/{id}`
- [ ] 11.8 Backend: implement every `/api/admin/*` endpoint guarded by an `[AdminOnly]` filter that requires `User.IsAdmin = true`
- [ ] 11.9 Integration tests for every scenario in `specs/admin-panel/spec.md`
- [ ] 11.10 Create `web_app_flow.md` documenting each admin route + corresponding backend endpoint

## 12. Cross-Cutting Hardening

- [ ] 12.1 Add Serilog structured logging for: auth events, post-create, media-finalize, feed reads (cached vs uncached), admin actions
- [ ] 12.2 Confirm rate-limit policies on every anonymous endpoint (`/auth/*`, `/contact`); add per-user rate limit on `/api/media/uploads` (max 20/hour)
- [ ] 12.3 Verify signed-URL upload buckets are private (no public list/get); CDN delivery uses signed read URLs or public read with random keys
- [ ] 12.4 Add an Admin → Maintenance action that resets a user's password (escape hatch for OQ — broken email inbox)
- [ ] 12.5 Resolve open questions OQ1 (R2 vs S3), OQ3 (video length cap), OQ4 (comment depth = 1), OQ5 (Turnstile), OQ6 (manual badge) with stakeholders; update specs/design only if decisions change

## 13. Definition of Done

- [ ] 13.1 All scenarios in every `specs/*/spec.md` are covered by passing integration tests
- [ ] 13.2 PRD §11 performance budgets measured locally: feed (cached) < 2 s, upload start < 1 s, app cold start < 3 s
- [ ] 13.3 `mobile_app_flow.md`, `backend_api_flow.md`, and `web_app_flow.md` updated for every module landed
- [ ] 13.4 Manual end-to-end smoke: register → verify OTP → open Feed (admin-seeded) → follow another test user → capture and post a photo + a multi-photo + a video → like / comment / save / share → admin publishes a Fake-vs-Real post → mobile sees it in Fake vs Real tab → admin moderates a post → user receives moderated state
- [ ] 13.5 `openspec status --change mvp-foundation` reports all artifacts done and apply-ready
