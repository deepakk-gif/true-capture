## Context

True Capture today is three skeleton folders. The Flutter app has a working email/password sign-in screen wired to endpoint paths that do not exist on the backend; the .NET backend implements `/api/auth/{register,login,refresh,logout}` with JWT + refresh-token rotation, captcha, and rate limiting, but nothing else; the Next.js folder is empty. The product vision (`prd.md`) is a trust-first Instagram analog whose differentiator is editorial Fake-vs-Real posts and, later, AI-driven trust scoring. The Phase 1 MVP captured by this change is the minimum surface that makes the editorial workflow real: a user can sign up, land on a feed that is never empty (admin posts baseline), follow people, capture media in-app, post it, and engage with content; an admin can publish Fake-vs-Real reports and moderate.

Operational constraints:
- Solo-ish MVP team (1 Flutter, 1 backend, 1 web, 1 designer, part-time devops). Designs that require running 8 services locally are non-starters.
- The backend is structured as a **modular monolith** (`TrueCapture.Modules.*` under one solution). New work follows the same pattern.
- Mobile state is **Riverpod** with `BaseConsumerState<TWidget, TVM>` + `ScreenStateAware`. Adding a parallel pattern is rejected by convention.
- Capture metadata is collected now even though trust scoring lands in Phase 2 — we cannot retro-collect for posts made before that capability ships.

Stakeholders: Deepak (product + tech lead), editorial team (Fake-vs-Real authors), MVP devs.

## Goals / Non-Goals

**Goals:**
- One end-to-end vertical slice: sign up → land on admin-seeded feed → capture → post → engage → admin can publish Fake-vs-Real.
- A modular monolith that can be split into the microservices listed in PRD §8 *later* without rewriting domain logic — module boundaries chosen now to match those future service boundaries.
- Mobile/backend endpoint contract reconciled in one move; no perpetual drift.
- Capture metadata schema in place from day one so Phase 2 trust scoring has historical data to score.
- Admin panel is a thin Next.js shell — power tools, not polish.

**Non-Goals:**
- Trust scoring algorithm (Phase 2). The `trust_scores` and `ai_scan_results` tables are **not** created here; the columns that depend on them on `posts` are deferred.
- AI / deepfake / NSFW detection (Phase 4). No Python FastAPI service stood up under this change.
- Push notifications (Phase 3). Preference flags are stored but no delivery channel is wired.
- Chat / SignalR (Phase 3). The top-bar chat icon may be present but routes to an "Coming soon" placeholder.
- Search / Elasticsearch (Phase 3). Hashtag and mention indexes are persisted but no search endpoint is exposed.
- Public web post pages, SEO, analytics dashboards, monetization (Phase 5).
- Mobile-app onboarding polish beyond what is needed to reach the Home shell.

## Decisions

### D1. Modular monolith, microservice-shaped modules
**Decision:** Keep everything in `TrueCapture.Api` as one deployable. Each PRD §8 microservice corresponds to one `TrueCapture.Modules.<Name>` project with its own entities, services, controllers, EF model configurator, and DI extension method. Cross-module calls go through public service interfaces — never direct DbContext queries across modules.
**Why not microservices now:** A 1-backend-dev team running 10 services locally is the worst possible outcome at MVP scale. Modules give us the boundary discipline of microservices with the operational simplicity of a monolith. When a module needs to scale independently (Media transcoding is the obvious first candidate), we lift the project out.
**Alternative considered:** Microservices from day one — rejected on cost. Single namespace with no module boundaries — rejected because we know it will need to split.

### D2. Reconcile mobile auth paths to existing backend paths
**Decision:** Change the mobile `ApiEndpoints` constants from `/auth/sign-in`, `/auth/sign-up`, `/auth/refresh-token` to the existing `/api/auth/login`, `/api/auth/register`, `/api/auth/refresh`. Add the missing routes (`/api/auth/{send-otp, verify-otp, forgot-password, reset-password, google}`) on the backend rather than ever adding `/auth/sign-in` aliases. Local dev tokens are invalidated; we are pre-launch with no real users.
**Why:** The backend's existing paths follow ASP.NET conventions and are the ones documented in `backend_api_flow.md`. The mobile paths were placeholders. Aliasing forever guarantees future drift.
**Alternative considered:** Add `/auth/*` aliases on the backend — rejected because it bakes in two names for the same endpoint forever.

### D3. Capture metadata embedded at upload, validated server-side
**Decision:** The mobile camera flow attaches a signed payload to every upload containing: capture timestamp (device clock + server-skew-correctable), device fingerprint (model + OS + app build + per-install UUID), optional GPS, and a boolean `in_app_capture = true`. The backend stores this in `media_assets.capture_metadata` JSONB column. Trust scoring in Phase 2 reads this column; it is **not** scored now.
**Why:** Capture metadata is one-shot — once a post exists without it, it is lost forever. Collecting from day one is the cheap, irreversible part; scoring it later is the reversible part.
**Trade-off:** Slightly larger upload payload (~200 bytes) and code complexity on the camera flow that delivers no MVP feature. Worth it to avoid retroactive blindness in Phase 2.

### D4. Feed cold-start: admin posts are *always* eligible
**Decision:** The feed query is `posts WHERE is_admin_post = true OR author_id IN (SELECT followed_id FROM follows WHERE follower_id = @me)`, ordered by a (recency × engagement) score. A 0-follow user therefore sees only admin posts — never an empty state. Admin posts continue to appear after the user follows people; they are not deprioritized.
**Why:** PRD requirement: never an empty feed; the platform's editorial voice is its differentiator. Implementing this as a query rule (not a fallback path) means there is no edge case to forget.
**Alternative considered:** "If follow count = 0, return admin posts; else return followed posts" — rejected because admin posts disappearing the moment a user follows their first creator hides the Fake-vs-Real channel from exactly the users most likely to need it.

### D5. Redis cache per user, bounded invalidation
**Decision:** Cache the first N pages of the assembled feed under `feed:{user_id}` with a 5-minute TTL. Invalidate on: the user's own follow/unfollow, a new admin post (broadcast invalidation), or a new post from someone the user follows (lookup via a `follow_inverse` cache key `followers:{author_id}`). Do **not** invalidate on engagement (likes / comments) — the next cache rebuild picks up new counters.
**Why:** Avoids both the thundering-herd on admin-post publish and the cache-stampede on every like.
**Trade-off:** Engagement counters may be stale by up to 5 minutes in the cached feed view. Acceptable — counters are decorative, not load-bearing.

### D6. Media pipeline: signed URL → S3/R2 → worker
**Decision:** Mobile requests a signed upload URL from `/api/media/uploads`, PUTs the bytes directly to S3/R2 (Cloudflare R2 for media-egress cost), then calls `/api/media/finalize` with the upload ID, MIME type, and capture metadata. A background worker (Hangfire in-process for MVP) compresses images, transcodes video to HLS via FFmpeg, generates thumbnails, and flips `media_assets.status` from `pending` to `ready`. Posts are visible in the feed only when all their media are `ready`.
**Why:** Signed URLs keep large bytes off the API server. R2 avoids egress fees for CDN delivery. Hangfire in-process is the cheapest possible queue and can be lifted to a separate worker process later.
**Alternative considered:** Multipart POST through the API — rejected, doesn't scale and bottlenecks the API server.

### D7. Hashtag / mention parsing happens server-side at post-create
**Decision:** The client sends the raw caption. The backend extracts `#hashtag` and `@mention` tokens with a single regex on create, then writes rows to `post_hashtags` and `post_mentions`. Clients re-parse the same regex for rendering clickable tokens.
**Why:** Single source of truth for what counts as a tag. Server can normalize (lowercase) and validate (length, allowed chars) once.
**Alternative considered:** Client computes tag list, sends it alongside caption — rejected, lets malicious clients lie about indexed terms.

### D8. Admin panel is auth-shared, role-gated
**Decision:** The Next.js admin panel logs in against the same `/api/auth/login` endpoint; admin gating is `user.IsAdmin == true` (the existing flag on `User`). No separate admin auth surface. Admin endpoints live under `/api/admin/*` and require the `admin` permission.
**Why:** One auth system. The `IsAdmin` flag already exists on `User`. Separate admin auth would be three more flows we don't need.

### D9. Theme on mobile: stored locally, mirrored server-side
**Decision:** Theme preference (`light` / `dark` / `system`) is stored in `SharedPreferences` for instant boot, and also PATCHed to `/api/users/me/settings` so a fresh install on a new device picks up the user's preference at login.
**Why:** Local-first for cold-start performance (<3s); server-mirrored for cross-device consistency.

### D10. Fake-vs-Real tab is a filtered feed, not a separate entity
**Decision:** Fake-vs-Real posts are rows in `posts` with `is_admin_post = true AND is_fake_vs_real = true`. The Fake-vs-Real tab calls the feed endpoint with a `?channel=fake_vs_real` filter. The admin "Fake-vs-Real composer" is just a post-create form with those two flags pre-set.
**Why:** Same engagement primitives (like / comment / share / save) for free. No second pipeline to maintain. Posts can appear in both Feed and Fake-vs-Real without duplication.

## Risks / Trade-offs

- **Capture spoofing** → Capture metadata is collected but *not validated* in MVP (no server-side cross-checks, no watermark). A motivated attacker can forge the payload today. **Mitigation:** Phase 2 trust infrastructure adds watermarking, sensor validation, and replay detection. Don't claim "verified real" status in the UI until Phase 2.
- **HLS transcoding cost** → FFmpeg on the same box as the API will bottleneck under load. **Mitigation:** Hangfire queue is the abstraction; the worker can be lifted into a separate container the moment we see queue depth grow.
- **Redis cache invalidation correctness** → Cross-user invalidation on admin post publish requires broadcasting to potentially every cached feed. **Mitigation:** TTL of 5 minutes bounds incorrectness automatically. For admin posts, set TTL to 30 seconds on the next read after publish — accept brief over-rebuild.
- **Mobile/backend contract drift** → Renaming auth paths now risks re-introducing drift if anyone adds a new endpoint without updating both sides. **Mitigation:** `backend_api_flow.md` and `mobile_app_flow.md` updates are part of the definition of done for any task that touches an endpoint.
- **OTP delivery reliability** → Email-only OTP (no SMS fallback) for password reset means a user with a broken inbox is locked out. **Mitigation:** Accepted for MVP. Add admin "reset password for user" tool in admin panel as the escape hatch.
- **Google OAuth on iOS** → Apple's policy may require Sign in with Apple alongside Google. **Mitigation:** Detect submission rejection in TestFlight; if blocked, add Apple Sign-In as a follow-up task before App Store submission. Out of scope until we hit it.
- **Modular monolith discipline** → Without enforcement, modules will reach into each other's DbContext. **Mitigation:** ArchUnit-style test (or Roslyn analyzer) added in tasks that fails CI on cross-module entity references.
- **No analytics** → We will not know if any of this works after launch. **Mitigation:** Accept for MVP; Phase 5 adds the dashboards. Log enough structured events server-side to reconstruct usage from Serilog if needed.

## Migration Plan

There is no production data. Migration plan is the rollout sequence:

1. Backend Identity module extensions (OTP, forgot/reset, Google OAuth) ship first, behind no feature flag — pre-launch.
2. Mobile auth-endpoint rename ships in lockstep with backend; old paths are deleted immediately.
3. User / Posts / Media / Feed modules land in dependency order. Each module's EF migration is auto-applied on dev startup (`RunMigrationsOnStartup=true`) and explicitly applied in CI for staging.
4. Admin panel boots last — it depends on every backend module being live.
5. Rollback: drop the latest migration(s) per module; no data to preserve.

## Open Questions

- **OQ1:** Cloudflare R2 vs AWS S3 for MVP storage? PRD lists both. R2 is cheaper for egress but requires Cloudflare CDN coupling. **Proposed:** R2 to align with Cloudflare CDN already in the PRD. Confirm with devops.
- **OQ2:** Sign in with Apple — include in MVP or wait for App Store rejection? **Proposed:** Wait. Track as a follow-up.
- **OQ3:** Video length cap for MVP uploads? PRD does not specify. **Proposed:** 60 seconds, single resolution ladder (240p / 480p / 720p). Confirm with product.
- **OQ4:** Comment depth — PRD says "with replies" but does not specify depth. **Proposed:** One level of replies (Instagram-equivalent). Confirm with product before specs are sealed.
- **OQ5:** Captcha provider — currently `[RequireCaptcha]` is wired but provider is unspecified in code. **Proposed:** Cloudflare Turnstile (already in the Cloudflare stack). Confirm with devops.
- **OQ6:** Admin verification badge — granted manually for MVP or does any heuristic apply? **Proposed:** Manual only, granted via admin panel. No heuristic.
