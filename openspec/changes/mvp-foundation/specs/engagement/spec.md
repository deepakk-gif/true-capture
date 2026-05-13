## ADDED Requirements

### Requirement: Toggle like on a post
The system SHALL support liking and unliking a post via `POST /api/posts/{id}/like` (toggle). A user SHALL have at most one like per post enforced by a unique constraint on `likes(user_id, post_id)`. The post's `likes_count` SHALL be maintained in `posts` and adjusted atomically with the row write.

#### Scenario: Like a post
- **WHEN** an authenticated user POSTs `/api/posts/{id}/like` for a post they have not liked
- **THEN** the system inserts a `likes` row, increments `posts.likes_count`, and returns `{ liked: true, count: <new> }`

#### Scenario: Unlike a post
- **WHEN** an authenticated user POSTs `/api/posts/{id}/like` for a post they currently like
- **THEN** the system deletes the `likes` row, decrements `posts.likes_count`, and returns `{ liked: false, count: <new> }`

#### Scenario: Like a non-existent post
- **WHEN** the post id does not exist
- **THEN** the system returns `404 Not Found`

### Requirement: Comment on a post with one level of replies
The system SHALL support commenting via `POST /api/posts/{id}/comments` with `{ body, parent_comment_id? }`. Replies (`parent_comment_id` set) SHALL be limited to one level — a reply to a reply SHALL be rejected. Comments SHALL be soft-deletable by author or admin.

#### Scenario: Top-level comment
- **WHEN** a user POSTs `/api/posts/{id}/comments` with `{ body }` and no `parent_comment_id`
- **THEN** the system inserts a `comments` row with `parent_comment_id=null`, increments `posts.comments_count`, and returns the new comment

#### Scenario: Single-level reply
- **WHEN** a user POSTs `/api/posts/{id}/comments` with `parent_comment_id` referencing a top-level comment on the same post
- **THEN** the system inserts the reply

#### Scenario: Reject second-level reply
- **WHEN** a user POSTs `/api/posts/{id}/comments` with `parent_comment_id` referencing a comment that itself has a `parent_comment_id`
- **THEN** the system returns `400 Bad Request`

#### Scenario: Author deletes own comment
- **WHEN** a user DELETEs a comment they authored
- **THEN** the system soft-deletes the row (body becomes `[deleted]`, retains thread shape) and decrements `posts.comments_count`

### Requirement: List comments
The system SHALL expose `GET /api/posts/{id}/comments` returning cursor-paginated top-level comments with a count of replies. Replies SHALL be fetched via `GET /api/comments/{id}/replies`.

#### Scenario: Paginated comments
- **WHEN** a client GETs `/api/posts/{id}/comments?cursor=&limit=20`
- **THEN** the system returns up to 20 top-level comments ordered by `created_at ASC` with a next cursor and each comment's `replies_count`

### Requirement: Share a post
The system SHALL expose `POST /api/posts/{id}/share` which records a `shares` row, increments `posts.shares_count`, and returns a canonical share URL. The share URL SHALL be of the form `https://truecapture.app/p/{id}` (subject to web-platform availability).

#### Scenario: Share record and URL
- **WHEN** an authenticated user POSTs `/api/posts/{id}/share`
- **THEN** the system inserts a `shares` row, increments the counter, and returns `{ url }`

### Requirement: Save and unsave a post
The system SHALL support saving via `POST /api/posts/{id}/save` (toggle). A `saves` row SHALL be unique on `(user_id, post_id)`. Saved posts SHALL be listable via `GET /api/users/me/saves` (cursor-paginated).

#### Scenario: Save toggle
- **WHEN** an authenticated user POSTs `/api/posts/{id}/save` for a post not yet saved
- **THEN** the system inserts a `saves` row and returns `{ saved: true }`

#### Scenario: Unsave toggle
- **WHEN** an authenticated user POSTs `/api/posts/{id}/save` for a post they have already saved
- **THEN** the system deletes the `saves` row and returns `{ saved: false }`

#### Scenario: List saved
- **WHEN** an authenticated user GETs `/api/users/me/saves?cursor=&limit=20`
- **THEN** the system returns up to 20 saved posts ordered by save time descending

### Requirement: Counters are decorative, not load-bearing
The denormalized counters (`likes_count`, `comments_count`, `shares_count`) on `posts` MAY be momentarily stale (up to 5 minutes in cached feed reads — see feed capability) but SHALL eventually converge with the source-of-truth row counts. A reconciliation job SHALL be runnable on demand by admins to recompute counters from rows.

#### Scenario: Admin reconciles counters
- **WHEN** an admin POSTs `/api/admin/maintenance/reconcile-counters`
- **THEN** the system recomputes `likes_count`, `comments_count`, and `shares_count` for every post from row counts and returns the number of rows updated
