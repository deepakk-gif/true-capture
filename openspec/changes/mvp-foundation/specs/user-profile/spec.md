## ADDED Requirements

### Requirement: View own profile
The system SHALL expose `GET /api/users/me` returning the authenticated user's profile: id, username, display name, avatar URL, bio, followers count, following count, posts count, `IsVerified` flag, and `CreatedAt`.

#### Scenario: Authenticated user fetches own profile
- **WHEN** a client GETs `/api/users/me` with a valid access token
- **THEN** the system returns `200 OK` with the user's profile DTO

#### Scenario: Unauthenticated request
- **WHEN** a client GETs `/api/users/me` without a token
- **THEN** the system returns `401 Unauthorized`

### Requirement: View another user's public profile
The system SHALL expose `GET /api/users/{id}` returning a public profile DTO that excludes email and any settings. The response SHALL include a `followState` field (`none` | `following` | `requested`) computed against the caller, a `followsMe` boolean, an `isMe` boolean, and a `canViewContent` boolean (true when the profile is public, the caller themselves, or an accepted follower of a private account).

> **MVP amendment (2026-05-21):** profiles are addressed by numeric `id`, not `username` — usernames are immutable but ids are the canonical key used across the mobile app and admin panel. The simple `you_follow` boolean was widened to `followState` to carry the pending follow-request state for private accounts.

#### Scenario: Fetch existing profile
- **WHEN** an authenticated client GETs `/api/users/{id}` for an existing active user
- **THEN** the system returns `200 OK` with the public profile and `followState` reflecting the caller's follow state

#### Scenario: Profile not found
- **WHEN** the id does not match any user
- **THEN** the system returns `404 Not Found`

### Requirement: Edit profile
The system SHALL allow the authenticated user to update display name, bio, gender, and account type (public/private) via `PUT /api/users/me`. Username SHALL be immutable in MVP. Avatar management uses dedicated endpoints: `POST /api/users/me/avatar` (multipart image upload, rate-limited) and `DELETE /api/users/me/avatar` (clear the avatar).

> **MVP amendment (2026-05-21):** avatar upload is a direct multipart upload to file storage, not the media-capture-upload signed-URL flow — that pipeline is not built under the current state of this change, and avatars need neither capture metadata nor transcoding. Profile edit is `PUT` (full replace of the editable fields), not `PATCH`. The request body carries no `username` field, so the immutability is structural.

#### Scenario: Update display name and bio
- **WHEN** a client PUTs `/api/users/me` with `{ displayName, bio }`
- **THEN** the system updates those fields, leaves all others unchanged, and returns the updated profile

#### Scenario: Username is not editable
- **WHEN** a client PUTs `/api/users/me`
- **THEN** the request body exposes no `username` field and the stored username is unchanged

### Requirement: Follow and unfollow
The system SHALL allow users to follow and unfollow each other via `POST /api/users/{id}/follow` and `DELETE /api/users/{id}/follow`. A user SHALL NOT be able to follow themselves. Follow edges live in a `Follow(follower_id, followee_id, status, created_at)` table with a unique constraint on the `(follower_id, followee_id)` pair.

> **MVP amendment (2026-05-21):** the follow edge carries a `status` (`accepted` | `pending`). Following a **public** account creates an `accepted` edge immediately; following a **private** account creates a `pending` edge — a follow request the followee accepts or rejects via `POST /api/follow/requests/{requesterId}/{accept|reject}`. `followState` reports `requested` for a pending edge. Only `accepted` edges count toward follower/following totals and content visibility.

#### Scenario: Follow a public account
- **WHEN** an authenticated client POSTs `/api/users/{id}/follow` for a different active public user they do not already follow
- **THEN** the system inserts an `accepted` follow edge and returns `200 OK` with `followState=following`

#### Scenario: Follow a private account
- **WHEN** an authenticated client POSTs `/api/users/{id}/follow` for a private user
- **THEN** the system inserts a `pending` follow edge and returns `200 OK` with `followState=requested`

#### Scenario: Follow self
- **WHEN** the caller targets their own id
- **THEN** the system returns `422 Unprocessable Entity`

#### Scenario: Idempotent follow
- **WHEN** the caller already has a follow edge to the target
- **THEN** the system returns `200 OK` with the existing follow state and inserts no duplicate row

#### Scenario: Unfollow
- **WHEN** the caller DELETEs `/api/users/{id}/follow` for a user they currently follow or have a pending request to
- **THEN** the system removes the follow edge and returns `200 OK` with `followState=none`

### Requirement: Followers and following lists
The system SHALL expose `GET /api/users/{id}/followers` and `GET /api/users/{id}/following` returning cursor-paginated lists of public profile summaries. When the target account is private and the caller is neither the owner nor an accepted follower, the system SHALL return `403 Forbidden`.

#### Scenario: List followers
- **WHEN** a client GETs `/api/users/{id}/followers?cursor=<opaque>`
- **THEN** the system returns a page of profile summaries ordered by follow time descending, plus a next-page cursor

#### Scenario: Private account blocks the list
- **WHEN** a non-follower GETs `/api/users/{id}/followers` for a private account
- **THEN** the system returns `403 Forbidden`

### Requirement: Mobile profile tab renders own profile
The Flutter mobile app SHALL render the signed-in user's profile under the Profile tab: avatar, username, display name, bio, followers count, following count, edit-profile entry point, posts grid, and settings entry points (account, theme, notifications, privacy, CMS pages, Contact Us, logout).

#### Scenario: Open Profile tab
- **WHEN** a signed-in user taps the Profile tab
- **THEN** the screen calls `GET /api/users/me`, displays the header and posts grid, and lists every settings entry point named above

#### Scenario: Top app-bar avatar opens own profile
- **WHEN** a signed-in user taps the avatar in the persistent top app bar
- **THEN** the app navigates to the Profile tab

### Requirement: Restriction state visible to the affected user
The `users` table SHALL have `RestrictionLevel` (enum: `none`, `restricted`, `muted`, `suspended`; default `none`) and `RestrictionExpiresAtUtc` (nullable). `GET /api/users/me` SHALL include both fields. The mobile app SHALL render a banner on the Profile tab when `RestrictionLevel != 'none'` explaining the state and (if `RestrictionExpiresAtUtc` is set) the expiry date. Public profile views (`GET /api/users/{username}`) SHALL NOT expose these fields to other users.

#### Scenario: Restricted user sees a banner
- **WHEN** a user whose `RestrictionLevel='restricted'` opens their Profile tab
- **THEN** a banner reads "Your account is restricted: you can read posts but cannot post or comment" and persists until the admin clears the restriction

#### Scenario: Suspended user with expiry
- **WHEN** a user whose `RestrictionLevel='suspended'` with `RestrictionExpiresAtUtc` set 5 days in the future opens the app
- **THEN** sign-in is blocked at `/api/auth/login` with reason `account_suspended` until the expiry passes

#### Scenario: Public profile hides restriction state
- **WHEN** any user other than the subject GETs `/api/users/{username}`
- **THEN** the response does NOT include `restrictionLevel` or `restrictionExpiresAtUtc`
