## ADDED Requirements

### Requirement: Admin panel hosted in Next.js 15
The system SHALL ship a Next.js 15 admin panel under `true_capture_web/` (App Router, server components by default) served from a separate hostname (e.g., `admin.truecapture.app`). The panel SHALL share the same `/api/auth/login` flow as the mobile app — there is NO separate admin auth surface.

#### Scenario: Admin login
- **WHEN** an admin opens the admin URL and submits email + password
- **THEN** the panel calls `POST /api/auth/login`, stores access + refresh tokens in HTTP-only cookies, and routes to the dashboard

#### Scenario: Non-admin sign-in blocked from admin
- **WHEN** a user with `IsAdmin=false` signs in to the admin panel
- **THEN** the panel logs them out, displays "Admin access required", and returns to the login screen

### Requirement: User moderation
The admin panel SHALL provide a user list searchable by email or username, with row-level actions: ban (sets `User.IsActive=false`), unban (sets `IsActive=true`), grant verification badge (`IsVerified=true`), revoke badge. Each action SHALL hit `PATCH /api/admin/users/{id}` with a structured payload.

#### Scenario: Ban a user
- **WHEN** an admin clicks "Ban" for a user
- **THEN** the panel PATCHes `/api/admin/users/{id}` with `{ isActive: false }`; the API updates the row and revokes all active refresh tokens for that user

#### Scenario: Grant verification badge
- **WHEN** an admin clicks "Verify"
- **THEN** the panel PATCHes `/api/admin/users/{id}` with `{ isVerified: true }`

#### Scenario: Banned user cannot use refresh tokens
- **WHEN** a banned user attempts `POST /api/auth/refresh`
- **THEN** the system returns `401 Unauthorized`

### Requirement: Post moderation
The admin panel SHALL provide a post review queue (newest first, filterable by `is_admin_post`, `is_fake_vs_real`, author username). Per-row actions SHALL include hide (soft) and delete (hard). Hidden posts SHALL be excluded from feed reads but retained in the database.

#### Scenario: Hide a post
- **WHEN** an admin clicks "Hide" on a post in the review queue
- **THEN** the panel PATCHes `/api/admin/posts/{id}` with `{ hidden: true }`; the post is excluded from `GET /api/feed` and `GET /api/users/{username}/posts` for non-admin callers

#### Scenario: Delete a post (admin override)
- **WHEN** an admin clicks "Delete" on any post (regardless of authorship)
- **THEN** the panel DELETEs `/api/posts/{id}`; the API cascades to all dependent rows

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
