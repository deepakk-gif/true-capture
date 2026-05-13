## ADDED Requirements

### Requirement: Theme preference (light / dark / system)
The Flutter app SHALL support three theme modes — `light`, `dark`, `system` — selectable from Profile → Settings → Theme. The chosen mode SHALL be persisted in `SharedPreferences` under a stable key and applied at app cold-start before the first frame to avoid theme flash.

#### Scenario: Switch to dark mode
- **WHEN** a user selects "Dark" in Theme settings
- **THEN** the app updates the active `ThemeMode`, persists `dark` to `SharedPreferences`, and re-renders the current screen in the dark palette

#### Scenario: System mode follows OS
- **WHEN** Theme is set to `system` and the user changes the OS appearance
- **THEN** the app re-renders in the new OS-driven palette on the next frame

#### Scenario: Cold-start without flash
- **WHEN** the app launches with theme persisted as `dark`
- **THEN** the first painted frame is rendered in dark palette (no light-themed flash)

### Requirement: Server-mirrored settings
The backend SHALL expose `GET /api/users/me/settings` and `PATCH /api/users/me/settings` over a `user_settings(user_id, theme, notification_prefs JSONB, privacy_prefs JSONB, updated_at)` table. The mobile app SHALL mirror local theme changes by PATCHing the new value (best-effort; failure SHALL NOT block the UI change).

#### Scenario: Mirror theme to server
- **WHEN** a user changes their theme on device A
- **THEN** the app PATCHes `/api/users/me/settings` with `{ theme: <new> }` and the server persists it

#### Scenario: Restore preference on new device
- **WHEN** a user signs in on a new device for the first time
- **THEN** the app GETs `/api/users/me/settings`, applies the server's theme value, and persists it locally

### Requirement: Notification preference storage (no delivery yet)
The system SHALL accept and persist notification preference flags `{ likes, comments, follows, mentions, fake_vs_real }` (each boolean, default `true`) in `user_settings.notification_prefs`. No delivery channel (FCM / APNS) is wired under this change — preferences are stored for use by the Phase 3 notifications capability.

#### Scenario: Disable like notifications
- **WHEN** a user PATCHes `/api/users/me/settings` with `notification_prefs.likes=false`
- **THEN** the system stores the new value and returns the updated settings

#### Scenario: Read preferences
- **WHEN** a client GETs `/api/users/me/settings`
- **THEN** the response includes `notification_prefs` with all five keys, defaulted to `true` if never set

### Requirement: Privacy toggles
The system SHALL accept privacy preference flags `{ profile_visibility: "public"|"private", allow_mentions: boolean }` persisted in `user_settings.privacy_prefs`. In MVP only `allow_mentions` SHALL have a behavioral effect — when `false`, the post-create flow SHALL skip inserting `post_mentions` rows that target this user.

#### Scenario: Block mentions
- **WHEN** user B sets `privacy_prefs.allow_mentions=false` and user A then creates a post with caption `"hi @b"`
- **THEN** no `post_mentions` row is inserted for B (the caption text is still stored)

### Requirement: Logout from settings
The Flutter app SHALL expose a "Log out" entry in Profile settings that calls `POST /api/auth/logout` with the current refresh token, clears local storage (tokens, cached user, theme is preserved), and navigates to the sign-in screen.

#### Scenario: Log out
- **WHEN** a signed-in user taps "Log out"
- **THEN** the app calls the logout endpoint, clears `accessToken`/`refreshToken`/cached user from storage, and navigates to `routeSignIn`

#### Scenario: Theme persists across logout
- **WHEN** a user with theme set to `dark` logs out
- **THEN** the sign-in screen renders in the dark palette
