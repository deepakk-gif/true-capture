## ADDED Requirements

### Requirement: Admin panel hosted in its own Next.js 15 app
The system SHALL ship a Next.js 15 admin panel at `true_capture_admin_panel/` (App Router, TypeScript, server components by default) deployed as a distinct application from the public web surface at `true_capture_web/` and served from a separate hostname (e.g., `admin.truecapture.app`). The panel SHALL share the same `/api/auth/login` flow as the mobile app — there is NO separate admin auth surface. The public web surface at `true_capture_web/` SHALL NOT contain admin routes.

#### Scenario: Admin login
- **WHEN** an admin opens the admin URL and submits email + password
- **THEN** the panel calls `POST /api/auth/login`, stores access + refresh tokens in HTTP-only cookies, and routes to the dashboard

#### Scenario: Non-admin sign-in blocked from admin
- **WHEN** a user with `IsAdmin=false` signs in to the admin panel
- **THEN** the panel logs them out, displays "Admin access required", and returns to the login screen

### Requirement: User moderation — ban / restrict / mute / suspend / verify
The admin panel SHALL provide a user list searchable by email or username, with row-level actions: ban (sets `User.IsActive=false` permanently), unban, **restrict** (`RestrictionLevel='restricted'` — user can read but cannot post or comment), **mute** (`RestrictionLevel='muted'` — user's posts and comments are hidden from non-followers), **suspend** (`RestrictionLevel='suspended'` with `RestrictionExpiresAtUtc` — user cannot login until expiry), clear restriction, grant verification badge (`IsVerified=true`), revoke badge. Each action SHALL hit `PATCH /api/admin/users/{id}` with a structured payload and SHALL write an `admin_audit_log` row.

#### Scenario: Ban a user
- **WHEN** an admin clicks "Ban" for a user
- **THEN** the panel PATCHes `/api/admin/users/{id}` with `{ isActive: false }`; the API updates the row, revokes all active refresh tokens for that user, and writes an `admin_audit_log` row

#### Scenario: Restrict a user (cannot post)
- **WHEN** an admin sets `restrictionLevel: "restricted"` on a user
- **THEN** the API updates `User.RestrictionLevel='restricted'`; subsequent `POST /api/posts` calls by that user return `403 Forbidden` with reason code `user_restricted`

#### Scenario: Suspend a user with expiry
- **WHEN** an admin PATCHes `{ restrictionLevel: "suspended", restrictionExpiresAtUtc: <utc-7d> }`
- **THEN** the API updates both fields, revokes active refresh tokens, and `/api/auth/login` returns `401 Unauthorized` with reason `account_suspended` until expiry passes

#### Scenario: Grant verification badge
- **WHEN** an admin clicks "Verify"
- **THEN** the panel PATCHes `/api/admin/users/{id}` with `{ isVerified: true }`

#### Scenario: Banned user cannot use refresh tokens
- **WHEN** a banned user attempts `POST /api/auth/refresh`
- **THEN** the system returns `401 Unauthorized`

### Requirement: Post moderation — hide / remove / sensitive-veil
The admin panel SHALL provide a post review queue (newest first, filterable by `is_admin_post`, `is_fake_vs_real`, `status`, `sensitive`, author username). Per-row actions SHALL include: **hide** (soft, reversible — sets `Post.Hidden=true`); **remove** (hard, sets `Post.Status='removed'`, retains row for audit, excludes from every non-admin read); **mark sensitive** / **unmark sensitive** (toggles `Post.Sensitive`); **delete** (full row deletion, admin override path on `DELETE /api/posts/{id}`). Every action SHALL write an `admin_audit_log` row.

#### Scenario: Hide a post
- **WHEN** an admin clicks "Hide" on a post in the review queue
- **THEN** the panel PATCHes `/api/admin/posts/{id}` with `{ hidden: true }`; the post is excluded from `GET /api/feed` and `GET /api/users/{username}/posts` for non-admin callers

#### Scenario: Remove a post (hard moderation, audited)
- **WHEN** an admin clicks "Remove" on a post with a removal reason
- **THEN** the panel PATCHes `/api/admin/posts/{id}` with `{ status: "removed", removalReason: "<reason>" }`; the post's `Status` flips to `removed` and an `admin_audit_log` row is written; non-admin reads no longer see the post

#### Scenario: Mark a post as sensitive
- **WHEN** an admin toggles "Sensitive" on a post
- **THEN** the panel PATCHes `/api/admin/posts/{id}` with `{ sensitive: true }`; the post continues to appear in feeds but mobile clients SHALL render the post with a tap-to-reveal blur overlay

#### Scenario: Delete a post (admin override)
- **WHEN** an admin clicks "Delete" on any post (regardless of authorship)
- **THEN** the panel DELETEs `/api/posts/{id}`; the API cascades to all dependent rows

### Requirement: Pre-publish content approval queue
The system SHALL support a policy-driven pre-publish approval queue. The `approval_policies` table SHALL define rules (e.g., `author_age_days_lt`, `caption_contains_banned_hashtag`); when a post-create request matches any policy, the post SHALL be inserted with `Status='pending_review'` rather than `live`. The admin panel SHALL provide a queue listing all `pending_review` posts with per-row Approve / Reject actions. Approval transitions `Status` to `live` and makes the post immediately eligible for the feed. Rejection transitions `Status` to `rejected` with an admin-supplied reason and SHALL NOT make the post visible to anyone except the author and admins.

#### Scenario: Post enters approval queue on policy match
- **WHEN** a user with account age < 7 days (matching the default policy) creates a post
- **THEN** the post is inserted with `Status='pending_review'`, does NOT appear in any feed, and surfaces in the admin approval queue

#### Scenario: Approve a pending post
- **WHEN** an admin clicks "Approve" on a queued post
- **THEN** the panel PATCHes `/api/admin/posts/{id}/approval` with `{ decision: "approve" }`; the post transitions to `Status='live'`, becomes eligible for feeds, and an audit row is written

#### Scenario: Reject a pending post
- **WHEN** an admin clicks "Reject" with reason "spam"
- **THEN** the panel PATCHes `/api/admin/posts/{id}/approval` with `{ decision: "reject", reason: "spam" }`; the post transitions to `Status='rejected'`, the author can see it on their profile with a rejection notice, and non-authors do not see it

#### Scenario: Default-flow post bypasses queue
- **WHEN** a user whose account is older than 7 days (matching no policy) creates a post with a clean caption
- **THEN** the post is inserted with `Status='live'` immediately, exactly as before this requirement

### Requirement: User analytics dashboard (Postgres-view-backed)
The admin panel SHALL provide a read-only analytics dashboard exposing: daily active users (DAU), weekly active users (WAU), 7-day rolling retention, signups by day, posts by day, uploads by day (including success-rate per `media_assets.status`), top creators by 7-day follower delta, Fake-vs-Real post engagement (likes + comments + shares per FvR post). The backend SHALL expose these as `GET /api/admin/analytics/{view}` reading from named Postgres views (`vw_dau`, `vw_wau`, `vw_retention_7d`, `vw_signups_daily`, `vw_posts_daily`, `vw_uploads_daily`, `vw_top_creators_7d`, `vw_fvr_engagement`). No event-stream pipeline is required in MVP.

#### Scenario: DAU view
- **WHEN** an admin GETs `/api/admin/analytics/dau?from=2026-04-01&to=2026-04-30`
- **THEN** the API returns one row per day with a `count` field derived from `vw_dau` restricted to that date range

#### Scenario: Top creators view
- **WHEN** an admin GETs `/api/admin/analytics/top-creators?days=7&limit=10`
- **THEN** the API returns 10 user summaries ordered by follower-count delta over the last 7 days

#### Scenario: Non-admin denied analytics
- **WHEN** a non-admin GETs any `/api/admin/analytics/*` endpoint
- **THEN** the system returns `403 Forbidden`

### Requirement: Broadcast / targeted announcements
The system SHALL support admin-authored announcements stored in `admin_announcements` (`Id`, `Title`, `Body`, `CtaUrl?`, `Audience` (enum: `all_users`, `active_7d`, `active_30d`, `never_active`, `single_user`), `TargetUserId?`, `CreatedByAdminId`, `CreatedAtUtc`, `ExpiresAtUtc?`). The admin panel SHALL provide a composer (title + body + optional CTA URL + audience picker). Mobile clients SHALL poll `GET /api/me/announcements?cursor=&limit=20` and SHALL be able to mark announcements read via `POST /api/me/announcements/{id}/read`.

#### Scenario: Publish broadcast to all users
- **WHEN** an admin submits the composer with audience `all_users`
- **THEN** the panel POSTs `/api/admin/announcements`; the row is persisted with `Audience='all_users'`; every user's next `GET /api/me/announcements` call sees the row until they mark it read

#### Scenario: Target single user
- **WHEN** an admin sets audience to `single_user` with a target user id
- **THEN** the row is persisted with `Audience='single_user'` and `TargetUserId` set; only that user receives it via their `/api/me/announcements` poll

#### Scenario: Mark announcement read
- **WHEN** a mobile client POSTs `/api/me/announcements/{id}/read`
- **THEN** the system records the read state (in an `announcement_reads(user_id, announcement_id, read_at_utc)` table) and the announcement no longer appears in subsequent unread fetches

### Requirement: Admin-to-user email send
The admin panel SHALL provide an email composer (subject, body, optional HTML preview) with audience picker (single user, all users). Submission SHALL hit `POST /api/admin/emails` which dispatches via the existing `IEmailSender` (the same provider used for OTP). Submissions SHALL be rate-limited per admin (max 5 broadcast sends per day; single-user sends max 50 per day). Every send SHALL produce one row in `admin_audit_log` plus one row per recipient in `admin_email_dispatches` for delivery tracking.

#### Scenario: Send to single user
- **WHEN** an admin submits the composer with audience `single_user`, a target user id, subject, and body
- **THEN** the system queues exactly one email through `IEmailSender`, writes one `admin_email_dispatches` row, and writes one `admin_audit_log` row

#### Scenario: Broadcast email
- **WHEN** an admin submits with audience `all_users`
- **THEN** the system enqueues one email per active user (status `IsActive=true`) and writes one `admin_email_dispatches` row per recipient

#### Scenario: Rate-limited broadcast
- **WHEN** the same admin submits a 6th broadcast within 24 hours
- **THEN** the system returns `429 Too Many Requests`

### Requirement: Taxonomy / hashtag management
The system SHALL maintain `banned_hashtags(tag, banned_by_admin_id, reason?, created_at_utc)` and `featured_hashtags(tag, featured_by_admin_id, featured_until_utc?, created_at_utc)`. Posts whose caption contains a banned hashtag SHALL match the approval policy `caption_contains_banned_hashtag` (and therefore enter `Status='pending_review'`). The admin panel SHALL provide add / remove UIs for both tables.

#### Scenario: Ban a hashtag
- **WHEN** an admin adds `#scam` to banned hashtags
- **THEN** subsequent posts containing `#scam` enter the approval queue rather than going live; the audit log records the addition

#### Scenario: Feature a hashtag with expiry
- **WHEN** an admin marks `#ladakhsunrise` as featured until a future UTC date
- **THEN** the row appears in `featured_hashtags` with that expiry; expired rows are excluded from queries that surface featured tags

### Requirement: Per-user data export
The system SHALL expose `POST /api/admin/users/{id}/export` which assembles a JSON document containing the user's profile, all their posts (including caption + media URLs + counters), their comments, likes, saves, shares, follows, and settings. The export SHALL be persisted to object storage and a short-lived signed URL returned. The action SHALL be audited.

#### Scenario: Export a user
- **WHEN** an admin POSTs `/api/admin/users/{id}/export`
- **THEN** the system enqueues a background job that builds the JSON, uploads it to storage, and returns `{ signedUrl, expiresAtUtc }`; an `admin_audit_log` row is written with the target user id

### Requirement: Append-only admin audit log
The system SHALL maintain `admin_audit_log(Id, AdminUserId, Action, TargetType, TargetId, PayloadJson, CreatedAtUtc)`. Every admin mutation across the admin panel SHALL write exactly one row. The admin panel SHALL expose `GET /api/admin/audit?cursor=&adminId?=&action?=&targetType?=` to list rows with filters.

#### Scenario: Action writes audit row
- **WHEN** an admin performs any mutation (ban, restrict, approve, reject, hide, remove, broadcast, email, taxonomy edit, CMS edit, export, etc.)
- **THEN** exactly one `admin_audit_log` row is written within the same transaction as the mutation

#### Scenario: Filter audit by admin
- **WHEN** an admin GETs `/api/admin/audit?adminId=<id>&limit=50`
- **THEN** the response contains up to 50 rows authored by that admin, most recent first

### Requirement: Fake-vs-Real composer
The admin panel SHALL provide a Fake-vs-Real composer that publishes posts with `is_admin_post=true, is_fake_vs_real=true` pre-set. The composer SHALL accept a caption (markdown not required), up to 10 media uploads (using the same signed-URL pipeline as mobile), and a "Verdict" field (`real` | `fake` | `misleading`) rendered prominently on the post card.

#### Scenario: Publish a Fake-vs-Real report
- **WHEN** an admin fills in caption, uploads 2 images, selects verdict "fake", and submits
- **THEN** the panel POSTs `/api/admin/posts` with the media asset ids, caption, `verdict=fake`, `is_admin_post=true`, `is_fake_vs_real=true`; the post becomes immediately eligible for the Fake-vs-Real tab

#### Scenario: Verdict persisted on post
- **WHEN** a Fake-vs-Real post is created with `verdict=fake`
- **THEN** the `posts` row stores `verdict` as a nullable enum column readable by mobile clients

### Requirement: CMS page editor
The admin panel SHALL provide a CMS page editor listing all `cms_pages` rows with inline title and Markdown body editing. Saving SHALL PUT `/api/admin/cms/pages/{slug}`.

#### Scenario: Edit Privacy Policy
- **WHEN** an admin opens the CMS list, selects `privacy-policy`, edits the body, and clicks Save
- **THEN** the panel PUTs `/api/admin/cms/pages/privacy-policy` with the new title and body; the API updates the row

### Requirement: Contact Us inbox
The admin panel SHALL provide a Contact Us inbox listing `contact_messages` filterable by status, with per-row status transitions (`open` → `in_progress` → `resolved`, or `spam`).

#### Scenario: Triage a ticket
- **WHEN** an admin opens a ticket and clicks "Mark in progress"
- **THEN** the panel PATCHes `/api/admin/contact/{id}` with `{ status: "in_progress" }`

### Requirement: Admin endpoints are gated by IsAdmin
Every endpoint under `/api/admin/*` SHALL require an authenticated user with `User.IsAdmin=true`. Non-admin callers SHALL receive `403 Forbidden`.

#### Scenario: Non-admin hits admin endpoint
- **WHEN** an authenticated non-admin user calls any `/api/admin/*` route
- **THEN** the system returns `403 Forbidden` with `Result.Forbidden`
