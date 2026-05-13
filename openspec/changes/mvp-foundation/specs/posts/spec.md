## ADDED Requirements

### Requirement: Post entity supports single photo, multi-photo, and video
The system SHALL model a post as a row in `posts` referencing one or more `media_assets` via `post_media(post_id, media_asset_id, position)`. A post's kind SHALL be derived from its media: `photo` (one media), `carousel` (≥ 2 photo media), or `video` (one video media). Mixing video with photos in one post is NOT allowed in MVP.

#### Scenario: Create single-photo post
- **WHEN** an authenticated user POSTs `/api/posts` with one ready photo media asset and a caption
- **THEN** the system inserts a `posts` row plus one `post_media` row at position 0 and returns the post DTO

#### Scenario: Create carousel post
- **WHEN** an authenticated user POSTs `/api/posts` with two or more ready photo media assets
- **THEN** the system inserts `post_media` rows in submitted order and the response DTO marks `kind=carousel`

#### Scenario: Reject mixed media
- **WHEN** a user POSTs a post with both video and photo media
- **THEN** the system returns `400 Bad Request`

### Requirement: Admin-post and Fake-vs-Real flags
The `posts` table SHALL have `is_admin_post BOOLEAN NOT NULL DEFAULT false` and `is_fake_vs_real BOOLEAN NOT NULL DEFAULT false`. Only users with `User.IsAdmin = true` SHALL be allowed to create posts with either flag set. `is_fake_vs_real = true` SHALL imply `is_admin_post = true` (database CHECK constraint).

#### Scenario: Non-admin cannot set admin flag
- **WHEN** a non-admin user POSTs `/api/posts` with `is_admin_post=true`
- **THEN** the system returns `403 Forbidden`

#### Scenario: Admin publishes Fake-vs-Real post
- **WHEN** an admin POSTs `/api/admin/posts` with `is_admin_post=true` and `is_fake_vs_real=true`
- **THEN** the system inserts the row and the post becomes eligible for both the Feed and the Fake-vs-Real tab

#### Scenario: Database rejects inconsistent flags
- **WHEN** any insert sets `is_fake_vs_real=true` with `is_admin_post=false`
- **THEN** the database CHECK constraint rejects the row

### Requirement: Caption parsing for hashtags and mentions
On post creation the backend SHALL extract `#hashtag` tokens (regex `#([a-zA-Z0-9_]{1,50})`) and `@mention` tokens (regex `@([a-zA-Z0-9_]{1,30})`) from the caption, lowercase them, and persist rows into `post_hashtags(post_id, tag)` and `post_mentions(post_id, mentioned_user_id)`. Mentions SHALL resolve to a `User.Id` by exact username match; unresolved mentions SHALL NOT be persisted but the original caption text is preserved.

#### Scenario: Caption indexed
- **WHEN** a user creates a post with caption `"Sunrise at #ladakh with @deepak"`
- **THEN** `post_hashtags` contains `(post_id, "ladakh")` and `post_mentions` contains `(post_id, <deepak's user id>)` if that user exists

#### Scenario: Unknown @ ignored for indexing
- **WHEN** the caption contains `@nonexistent_user`
- **THEN** no `post_mentions` row is inserted, but the caption is stored verbatim

#### Scenario: Hashtags lowercased
- **WHEN** a caption contains `#Ladakh` and `#LADAKH`
- **THEN** only one row `(post_id, "ladakh")` is inserted in `post_hashtags`

### Requirement: Read posts by id and by author
The system SHALL expose `GET /api/posts/{id}` returning the full post DTO (author summary, media URLs, caption, counters, `kind`, `is_admin_post`, `is_fake_vs_real`, `created_at`) and `GET /api/users/{username}/posts` returning a cursor-paginated list of that user's posts.

#### Scenario: Fetch single post
- **WHEN** a client GETs `/api/posts/{id}` for an existing post whose media is `ready`
- **THEN** the system returns the full post DTO with all media URLs

#### Scenario: Posts grid for profile
- **WHEN** a client GETs `/api/users/{username}/posts?cursor=&limit=24`
- **THEN** the system returns up to 24 posts ordered by `created_at` descending with a next cursor

### Requirement: Delete own post
The authenticated user SHALL be able to delete their own post via `DELETE /api/posts/{id}`. Admins SHALL be able to delete any post via the same route. Deletion SHALL hard-delete the `posts` row and cascade to `post_media`, `post_hashtags`, `post_mentions`, `comments`, `likes`, `saves`, `shares`.

#### Scenario: Author deletes own post
- **WHEN** a user DELETEs `/api/posts/{id}` for a post they authored
- **THEN** the system removes the post and all dependent rows and returns `204 No Content`

#### Scenario: Non-author non-admin deletion forbidden
- **WHEN** a user DELETEs a post they did not author and they are not an admin
- **THEN** the system returns `403 Forbidden`

### Requirement: Post status and sensitivity
The `posts` table SHALL have:
- `Status` enum column with values `live`, `pending_review`, `rejected`, `removed`; default `live`.
- `Sensitive BOOLEAN NOT NULL DEFAULT false`.
- `RemovalReason TEXT NULL`, populated when `Status='removed'` or `Status='rejected'`.

Posts SHALL only be inserted with `Status='pending_review'` when at least one row in `approval_policies` matches the create request (see admin-panel spec). All other create paths SHALL insert with `Status='live'`. Only admins may transition `Status` after creation.

#### Scenario: Default create is live
- **WHEN** a user with account age ≥ 7 days posts with a caption that does not match any approval policy
- **THEN** the post is inserted with `Status='live'`

#### Scenario: Policy match enters review
- **WHEN** a user matches an approval policy at post-create
- **THEN** the post is inserted with `Status='pending_review'` and is not visible in any non-admin feed read

#### Scenario: Sensitive flag toggled by admin
- **WHEN** an admin PATCHes `/api/admin/posts/{id}` with `{ sensitive: true }`
- **THEN** `Post.Sensitive` becomes `true`; the post is still returned by every feed it would otherwise be in

#### Scenario: Rejected post visible only to author and admins
- **WHEN** a post with `Status='rejected'` is requested via `GET /api/posts/{id}`
- **THEN** the API returns the post DTO with `status='rejected'` and `removalReason` set if the caller is the author or an admin; otherwise it returns `404 Not Found`

### Requirement: Mobile post card renders required primitives
The mobile post card SHALL render: author avatar + username (tap → author profile), media (with carousel indicator when `kind=carousel` and auto-play on view when `kind=video`), action row with Like / Comment / Share each showing counts, a right-aligned Save button, caption with clickable `#hashtag` and `@mention` tokens, and a relative post time formatted as: `< 60s` → `Just now`, `< 60m` → `Nm ago`, `< 24h` → `Nh ago`, `< 7d` → `Nd ago`, `< 52w` → `Nw ago`, else `Ny ago`.

#### Scenario: Tap hashtag in caption
- **WHEN** a user taps a `#tag` token inside a post caption
- **THEN** the app navigates to the hashtag results screen for that tag

#### Scenario: Tap mention in caption
- **WHEN** a user taps an `@username` token inside a post caption
- **THEN** the app navigates to that user's profile

#### Scenario: Relative time formatting
- **WHEN** a post was created 3 hours ago
- **THEN** the post card displays `3h ago`

### Requirement: Mobile renders sensitive-content veil
When the post DTO has `sensitive=true`, the mobile post card SHALL render the media inside a tap-to-reveal blur overlay with a "Show content" CTA. First tap SHALL reveal the media for the current session only — relaunching the app SHALL re-apply the veil.

#### Scenario: Veil on sensitive post
- **WHEN** the feed contains a post with `sensitive=true`
- **THEN** the card shows the post's metadata (author, caption, counters) unmodified and renders the media area as a blurred placeholder with "Show content"

#### Scenario: Reveal lasts the session
- **WHEN** the user taps "Show content" on a sensitive post and scrolls away then back
- **THEN** the media remains revealed; on next app cold-start the veil re-applies
