## ADDED Requirements

### Requirement: Personalized feed assembly
The system SHALL expose `GET /api/feed` returning posts where `is_admin_post = true` OR the author is in the caller's follow set. Results SHALL be ranked by a (recency × engagement) score, paginated by opaque cursor, and exclude posts whose media is not all `status=ready`.

#### Scenario: Mixed-feed user
- **WHEN** an authenticated user who follows 3 creators GETs `/api/feed?limit=20`
- **THEN** the response contains posts from those creators interleaved with admin posts, ranked by the combined score, with a next-page cursor

#### Scenario: Cursor pagination
- **WHEN** a client GETs `/api/feed?cursor=<cursor_from_prior_page>&limit=20`
- **THEN** the system returns the next 20 ranked posts after the cursor without duplicating earlier results

#### Scenario: Hide unready media
- **WHEN** the ranked window contains a post whose media is in `status=pending`
- **THEN** that post is skipped and the next candidate fills the slot

### Requirement: Cold-start feed shows admin posts only
A user with zero `follows` rows SHALL receive a feed composed entirely of admin posts. The system SHALL NOT return an empty feed for any authenticated user as long as at least one admin post exists.

#### Scenario: Brand-new account
- **WHEN** a user who has just registered and follows no one GETs `/api/feed`
- **THEN** the response contains the most recent admin posts ranked by recency

#### Scenario: No admin posts exist at all
- **WHEN** the database contains zero posts with `is_admin_post=true`
- **THEN** the system returns an empty list with `200 OK` (not an error)

### Requirement: Admin posts remain eligible after follows
Admin posts SHALL continue to appear in the feed regardless of the caller's follow count. The feed composition rule SHALL be `(admin posts) ∪ (followed-user posts)` — admin posts are never deprioritized merely because the user has follows.

#### Scenario: Established user still sees admin posts
- **WHEN** a user who follows 100 creators GETs `/api/feed`
- **THEN** recent admin posts appear in the response alongside followed-creator posts

### Requirement: Per-user Redis cache with bounded invalidation
The system SHALL cache the first N pages (configurable, default N=2 of 20 each) of the assembled feed under key `feed:{user_id}` with a TTL of 5 minutes. The cache SHALL be invalidated on: (a) the caller's own follow/unfollow, (b) creation of a new admin post (broadcast invalidate), (c) creation of a new post by an author the caller follows (looked up via `followers:{author_id}`). The cache SHALL NOT be invalidated on engagement events.

#### Scenario: Cache hit
- **WHEN** the same user fetches the feed twice within 5 minutes without intervening invalidation
- **THEN** the second response is served from Redis without a database query for the ranked window

#### Scenario: New admin post invalidates broadly
- **WHEN** an admin publishes a new post
- **THEN** the system invalidates all `feed:*` keys

#### Scenario: Follow invalidates own cache
- **WHEN** a user follows another user
- **THEN** the system deletes `feed:{user_id}` before the next read

### Requirement: Fake-vs-Real channel as a filtered view
The system SHALL expose `GET /api/feed?channel=fake_vs_real` returning posts where `is_fake_vs_real = true`, ordered by `created_at DESC`, paginated by cursor. The same engagement primitives apply.

#### Scenario: Fake-vs-Real tab fetch
- **WHEN** a client GETs `/api/feed?channel=fake_vs_real&limit=20`
- **THEN** the response contains only posts with `is_fake_vs_real=true`, most recent first

#### Scenario: Non-admin attempts to author into channel via normal endpoint
- **WHEN** a non-admin user POSTs `/api/posts` with `is_fake_vs_real=true`
- **THEN** the system returns `403 Forbidden` (see posts capability)

### Requirement: Only `Status='live'` posts appear in non-admin feeds
Every feed read (Feed tab, Fake-vs-Real tab, profile posts grid) SHALL filter `Status='live'` when the caller is not an admin and not the author. Posts with `Status` in (`pending_review`, `rejected`, `removed`) SHALL be excluded.

#### Scenario: Pending-review post hidden
- **WHEN** a non-author user fetches a feed that would otherwise include a post with `Status='pending_review'`
- **THEN** the post is excluded from the response

#### Scenario: Author sees own pending-review post on their profile
- **WHEN** the author fetches `GET /api/users/me/posts` and they have a `pending_review` post
- **THEN** the post is included with `status='pending_review'` so the mobile UI can show "Your post is awaiting review"

#### Scenario: Removed post hidden everywhere
- **WHEN** any non-admin caller would otherwise see a `Status='removed'` post
- **THEN** the post is excluded; only admin endpoints surface removed posts

### Requirement: Mobile renders Feed and Fake-vs-Real tabs
The mobile app's Home shell SHALL be a bottom-tab container with a persistent top app bar (left: profile avatar; right: chat button) and four tabs in order: Feed, Fake vs Real, Create Post, Profile. Tabs 1 and 2 SHALL render lists of post cards using the feed endpoint with and without the `channel=fake_vs_real` filter respectively.

#### Scenario: Switch to Fake-vs-Real tab
- **WHEN** a user taps the Fake vs Real tab
- **THEN** the screen fetches `GET /api/feed?channel=fake_vs_real` and renders only those posts

#### Scenario: Top app bar always visible
- **WHEN** a user is on any of the four tabs
- **THEN** the top app bar with profile avatar (left) and chat button (right) is shown
