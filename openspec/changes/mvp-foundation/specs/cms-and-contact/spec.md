## ADDED Requirements

### Requirement: CMS pages stored server-side
The system SHALL persist CMS pages in `cms_pages(slug, title, body_markdown, updated_at)` with a unique `slug`. The MVP set of slugs SHALL include `about`, `terms-of-service`, `privacy-policy`, `community-guidelines`. The system SHALL seed empty placeholders for these slugs on first migration.

#### Scenario: Seed on first migration
- **WHEN** the backend runs the CMS module's first migration
- **THEN** four rows with slugs `about`, `terms-of-service`, `privacy-policy`, `community-guidelines` exist (titles populated, bodies may be empty)

### Requirement: Public read of CMS pages
The system SHALL expose `GET /api/cms/pages/{slug}` returning `{ slug, title, body_markdown, updated_at }`. The endpoint SHALL be anonymous and cacheable (`Cache-Control: public, max-age=300`).

#### Scenario: Fetch existing page
- **WHEN** a client GETs `/api/cms/pages/privacy-policy`
- **THEN** the system returns `200 OK` with the page payload

#### Scenario: Unknown slug
- **WHEN** the slug is not in the table
- **THEN** the system returns `404 Not Found`

### Requirement: Admin update of CMS pages
The system SHALL allow admins to update CMS page body and title via `PUT /api/admin/cms/pages/{slug}` with `{ title, body_markdown }`. The endpoint SHALL require `User.IsAdmin=true`.

#### Scenario: Admin edits Terms
- **WHEN** an admin PUTs `/api/admin/cms/pages/terms-of-service` with new title and body
- **THEN** the row is updated and `updated_at` is refreshed

#### Scenario: Non-admin denied
- **WHEN** a non-admin attempts the same PUT
- **THEN** the system returns `403 Forbidden`

### Requirement: Contact Us submission
The system SHALL expose `POST /api/contact` accepting `{ subject, body, email? }` (email optional only when the caller is authenticated, in which case the user's email is used; required when anonymous). Submissions SHALL be persisted to `contact_messages(id, user_id?, email, subject, body, created_at, status)` with `status='open'`. The endpoint SHALL be rate-limited per IP (max 5 per hour) and captcha-gated for anonymous callers.

#### Scenario: Authenticated submission
- **WHEN** an authenticated user POSTs `/api/contact` with `{ subject, body }`
- **THEN** the system inserts a row with `user_id` and `email` from the user's profile and returns `200 OK`

#### Scenario: Anonymous submission with captcha
- **WHEN** an anonymous client POSTs `/api/contact` with `{ subject, body, email }` and a valid captcha header
- **THEN** the system inserts the row with `user_id=null` and returns `200 OK`

#### Scenario: Anonymous without captcha
- **WHEN** an anonymous client POSTs `/api/contact` without a valid captcha header
- **THEN** the system returns `400 Bad Request`

### Requirement: Admin inbox for contact messages
The system SHALL expose `GET /api/admin/contact` returning cursor-paginated `contact_messages` filtered by `status`, and `PATCH /api/admin/contact/{id}` to update `status` to one of `open`, `in_progress`, `resolved`, `spam`.

#### Scenario: List open tickets
- **WHEN** an admin GETs `/api/admin/contact?status=open&limit=50`
- **THEN** the system returns up to 50 open messages most recent first

#### Scenario: Mark resolved
- **WHEN** an admin PATCHes `/api/admin/contact/{id}` with `{ status: "resolved" }`
- **THEN** the row's status is updated and the response reflects the new state

### Requirement: Mobile renders CMS pages from server
The Flutter app's Profile settings SHALL link to four entries — About, Terms of Service, Privacy Policy, Community Guidelines — each opening a renderer that fetches `/api/cms/pages/{slug}` and renders the Markdown body. There SHALL be no app-bundled copies of these pages.

#### Scenario: Open Privacy Policy
- **WHEN** a user taps "Privacy Policy" in profile settings
- **THEN** the app GETs `/api/cms/pages/privacy-policy` and renders the markdown body

### Requirement: Mobile Contact Us form
The Flutter app SHALL render a Contact Us form under Profile settings that POSTs `{ subject, body }` to `/api/contact` and shows a success toast on `200 OK`.

#### Scenario: Submit contact form
- **WHEN** a signed-in user submits the Contact Us form with non-empty subject and body
- **THEN** the app POSTs to `/api/contact`, shows "Message sent" on success, and clears the form
