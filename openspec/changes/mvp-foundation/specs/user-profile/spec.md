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
The system SHALL expose `GET /api/users/{username}` returning a public profile DTO that excludes email and any settings. The response SHALL include a `you_follow` boolean computed against the caller.

#### Scenario: Fetch existing profile
- **WHEN** an authenticated client GETs `/api/users/{username}` for an existing active user
- **THEN** the system returns `200 OK` with the public profile and `you_follow` reflecting the caller's follow state

#### Scenario: Profile not found
- **WHEN** the username does not match any user
- **THEN** the system returns `404 Not Found`

### Requirement: Edit profile
The system SHALL allow the authenticated user to update display name, bio, and avatar via `PATCH /api/users/me`. Username SHALL be immutable in MVP. Avatar uploads SHALL go through the media-capture-upload signed-URL flow and be referenced by `media_asset_id`.

#### Scenario: Update display name and bio
- **WHEN** a client PATCHes `/api/users/me` with `{ displayName, bio }`
- **THEN** the system updates those fields, leaves all others unchanged, and returns the updated profile

#### Scenario: Reject username change
- **WHEN** a client PATCHes `/api/users/me` with a `username` field
- **THEN** the system returns `400 Bad Request`

### Requirement: Follow and unfollow
The system SHALL allow users to follow and unfollow each other via `POST /api/users/{username}/follow` and `DELETE /api/users/{username}/follow`. A user SHALL NOT be able to follow themselves. Follow rows live in a `follows(follower_id, followed_id, created_at)` table with a unique constraint on the pair.

#### Scenario: Follow another user
- **WHEN** an authenticated client POSTs `/api/users/{username}/follow` for a different active user they do not already follow
- **THEN** the system inserts a `follows` row and returns `200 OK` with the new follower/following counts

#### Scenario: Follow self
- **WHEN** the caller targets their own username
- **THEN** the system returns `400 Bad Request`

#### Scenario: Idempotent follow
- **WHEN** the caller is already following the target
- **THEN** the system returns `200 OK` without inserting a duplicate row

#### Scenario: Unfollow
- **WHEN** the caller DELETEs `/api/users/{username}/follow` for a user they currently follow
- **THEN** the system removes the `follows` row and returns `200 OK`

### Requirement: Followers and following lists
The system SHALL expose `GET /api/users/{username}/followers` and `GET /api/users/{username}/following` returning cursor-paginated lists of public profile summaries.

#### Scenario: List followers
- **WHEN** a client GETs `/api/users/{username}/followers?cursor=<opaque>&limit=20`
- **THEN** the system returns up to 20 profile summaries ordered by follow time descending, plus a next-page cursor

### Requirement: Mobile profile tab renders own profile
The Flutter mobile app SHALL render the signed-in user's profile under the Profile tab: avatar, username, display name, bio, followers count, following count, edit-profile entry point, posts grid, and settings entry points (account, theme, notifications, privacy, CMS pages, Contact Us, logout).

#### Scenario: Open Profile tab
- **WHEN** a signed-in user taps the Profile tab
- **THEN** the screen calls `GET /api/users/me`, displays the header and posts grid, and lists every settings entry point named above

#### Scenario: Top app-bar avatar opens own profile
- **WHEN** a signed-in user taps the avatar in the persistent top app bar
- **THEN** the app navigates to the Profile tab
