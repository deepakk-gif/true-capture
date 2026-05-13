## ADDED Requirements

### Requirement: In-app camera (no gallery picker)
The Flutter mobile app SHALL capture media exclusively through an in-app camera surface — there SHALL be no gallery picker in the Create flow for MVP. The camera SHALL support photo and video modes, a front/back toggle, a flash toggle, and basic pinch-to-zoom.

#### Scenario: Open Create tab
- **WHEN** a signed-in user taps the Create Post tab
- **THEN** the app opens the camera surface with photo mode selected by default, exposes front/back toggle, flash toggle, and zoom

#### Scenario: No gallery import
- **WHEN** a user looks for a "Choose from library" button on the Create surface
- **THEN** no such control exists

### Requirement: Embed capture metadata on every upload
For every captured media asset, the client SHALL attach a capture-metadata payload containing: device-local capture timestamp (ISO-8601 UTC), device fingerprint `{ model, os, osVersion, appBuild, installUuid }`, optional GPS `{ lat, lng, accuracy_m }`, and `in_app_capture: true`. The backend SHALL persist this payload into `media_assets.capture_metadata` as JSONB without scoring it in this change.

#### Scenario: Photo capture sends metadata
- **WHEN** a user captures a photo and submits the post
- **THEN** the multipart finalize call to `POST /api/media/finalize` contains a `capture_metadata` field with all required keys and `in_app_capture=true`

#### Scenario: GPS optional
- **WHEN** the user has denied location permission
- **THEN** the upload still succeeds; `capture_metadata.gps` is omitted and the record is accepted

### Requirement: Signed-URL upload pipeline
The system SHALL provide a two-step upload pipeline: `POST /api/media/uploads` returns a short-lived (≤ 15 minutes) pre-signed PUT URL to S3 / Cloudflare R2 plus an `upload_id`. After the client PUTs bytes directly to the URL, `POST /api/media/finalize` is called with `{ upload_id, mime_type, capture_metadata, intended_kind }` to create the `media_assets` row in `status=pending`.

#### Scenario: Request signed URL
- **WHEN** an authenticated client POSTs `/api/media/uploads` with `{ mime_type, byte_size, kind }` for an allowed MIME and a size under the policy
- **THEN** the system returns `{ upload_id, put_url, expires_at }` and reserves a storage key

#### Scenario: Reject oversize upload request
- **WHEN** a client requests a signed URL for a video larger than 200 MB (or photo larger than 25 MB)
- **THEN** the system returns `413 Payload Too Large`

#### Scenario: Finalize after PUT
- **WHEN** a client POSTs `/api/media/finalize` with a valid `upload_id` after the PUT succeeded
- **THEN** the system inserts a `media_assets` row with `status=pending`, enqueues a processing job, and returns the asset's id

### Requirement: Image compression and video transcoding
The backend SHALL run a background worker that, for each new `media_assets` row in `status=pending`: compresses photos to a single optimized JPEG/WebP, transcodes videos to HLS (3 resolution rungs: 240p, 480p, 720p) capped at 60 seconds, generates a thumbnail, and on success flips the row to `status=ready`. On failure it SHALL flip to `status=failed` with a stored error code.

#### Scenario: Photo processed
- **WHEN** the worker picks up a pending photo asset
- **THEN** it produces a compressed master and a thumbnail, writes both to storage, updates the row's URLs, and sets `status=ready`

#### Scenario: Video processed
- **WHEN** the worker picks up a pending video asset
- **THEN** it produces an HLS playlist with three rungs and a thumbnail, writes them to storage, updates URLs, and sets `status=ready`

#### Scenario: Video over 60 seconds
- **WHEN** the worker encounters a video whose duration exceeds 60 seconds
- **THEN** it sets `status=failed` with error code `duration_exceeded` and does not transcode

### Requirement: Posts gate on media readiness
A post SHALL appear in the feed only when every `media_assets` row it references is in `status=ready`. Posts with any `pending` media SHALL be hidden from public reads; posts with any `failed` media SHALL be returned to the author with a "processing failed" indicator and SHALL NOT appear in others' feeds.

#### Scenario: Post hidden while media pending
- **WHEN** a follower fetches the feed and a candidate post has media still in `status=pending`
- **THEN** that post is excluded from the response

#### Scenario: Post visible once ready
- **WHEN** all media for a post transition to `status=ready`
- **THEN** the post becomes eligible for the next feed-cache rebuild

### Requirement: Background upload queue with retry on mobile
The Flutter app SHALL queue uploads locally so that closing the Create screen does not lose the upload. The queue SHALL retry network failures with exponential backoff (1s, 4s, 16s, abort).

#### Scenario: Resume after backgrounding
- **WHEN** a user submits a post and immediately backgrounds the app
- **THEN** the upload continues from the queue; on next foreground the user sees the post's processing state on their profile
