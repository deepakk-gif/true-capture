# PRD — True Capture

- **Project Codename:** True Capture
- **Version:** 1.1
- **Prepared By:** Deepak

## Stack

| Layer | Technology |
|---|---|
| Mobile App | Flutter |
| Backend API | .NET 9 Web API |
| Database | PostgreSQL |
| Cache | Redis |
| Media / AI Processing | Python FastAPI service |
| Web Platform | Next.js 15 (SEO + speed) |
| Web Admin | Next.js 15 (separate app — operational console) |
| CDN | Cloudflare |
| Storage | AWS S3 / Cloudflare R2 |

---

## 1. Product Vision

Build a **trust-first social content platform** where users can confidently distinguish authentic content from AI-generated or manipulated media.

In today's world, AI-generated photos and videos go viral within minutes. They often carry misinformation, and ordinary users have no easy way to know what's real. **True Capture** solves the trust gap by:

- Encouraging genuine, in-app captured content
- Running an admin-curated **"Fake vs Real"** awareness section that publicly debunks viral misinformation
- Providing trust signals (verified creators, AI-detection scores) on every post

The product feels like Instagram in interaction, but its purpose is **awareness and trust**, not vanity.

---

## 2. The Problem

Modern social platforms have:

- AI-generated fake videos and photos
- Edited misinformation
- Reuploaded fake media presented as original
- No mechanism to validate authenticity

Users no longer know: *"Is this real?"*

True Capture answers that question — both algorithmically (trust scoring) and editorially (the Fake vs Real channel).

---

## 3. Product Goals

### Primary
- Build trust in user-generated media
- Run a public awareness channel (Fake vs Real) operated by the platform team
- Detect manipulated / AI-generated content at upload
- Grow a verified creator base

### Secondary
- High engagement social experience comparable to Instagram
- Fast media playback and scrolling
- SEO-discoverable web presence for viral awareness posts

---

## 4. Target Users

| Phase | Audience |
|---|---|
| Phase 1 | General social users, students, casual creators |
| Phase 2 | Journalists, fact-checkers, influencers |
| Phase 3 | News/media agencies, government verification, marketplace sellers |

---

## 5. Application Surfaces

True Capture ships on **four** surfaces. Each lives in its own folder:

| Surface | Folder | Purpose |
|---|---|---|
| Mobile app | `true_capture_app/` | Primary consumer experience (Flutter, iOS + Android) |
| Backend API | `true_capture_backend/` | .NET 9 Web API serving all clients |
| Public web | `true_capture_web/` | Next.js 15 — SEO landing, public post pages (no admin) |
| Admin panel | `true_capture_admin_panel/` | Next.js 15 — operational admin console (moderation, analytics, notifications, CMS, contact, taxonomy, exports, audit) |

Module/flow change tracking lives in:
- `mobile_app_flow.md`
- `backend_api_flow.md`
- `web_app_flow.md`
- `admin_panel_flow.md`

---

## 6. Mobile App — Information Architecture

### 6.1 Authentication

- **Sign up / sign in:** Email + password
- **Social login:** Google
- On successful login → navigate to **Home**
- Standard flows: forgot password, OTP verification (email), session refresh

### 6.2 Home (post-login root)

The home screen is a **bottom-tab container** with a persistent **top app bar**.

**Top app bar**
- **Left:** Profile avatar (taps open the user's own profile)
- **Right:** Chat button (opens Messaging — see Module 8)

**Bottom tabs**

| # | Tab | Purpose |
|---|---|---|
| 1 | Feed | Personalized post feed |
| 2 | Fake vs Real | Admin-only awareness posts |
| 3 | Create Post | In-app camera + caption composer |
| 4 | Profile | User profile + settings |

---

### Tab 1 — Feed

Instagram-style scrolling feed. Each post is one of:

- Single photo
- **Multi-photo carousel** (swipeable)
- Video (auto-play on view)

**Post card layout**
- Author avatar + username (tap → author profile)
- Media (with carousel indicator if multi-photo)
- Action row: **Like**, **Comment**, **Share** (each with count)
- **Save** button (right-aligned, like Instagram bookmark)
- Caption (with clickable `#hashtag` and `@mention`)
- **Relative post time** — adaptive format:
  - `< 60s` → "Just now"
  - `< 60m` → "Nm ago"
  - `< 24h` → "Nh ago"
  - `< 7d` → "Nd ago"
  - `< 52w` → "Nw ago"
  - else → "Ny ago"

**Feed composition rules**
- A user only sees posts from accounts they **follow**, **plus** admin posts
- A brand-new user (0 follows) therefore sees only admin posts — never an empty feed
- Once the user follows others, the feed mixes followed-user posts with admin posts, ranked by trend / engagement algorithm
- Admin posts are always eligible regardless of follow graph (acts as the platform's editorial baseline)

---

### Tab 2 — Fake vs Real

A dedicated awareness channel.

- **Only admin (platform owner) posts are visible here**
- Each post is a verification report: a viral piece of misinformation with the team's verdict (real vs fake) and explanation
- Same post-card primitives as the Feed (like, comment, share, save) so users can engage and amplify the truth
- Acts as the platform's editorial voice and its main differentiator

---

### Tab 3 — Create Post

- Capture media using the **in-app camera only** (no gallery picker for first release)
  - Photo and video modes
  - Front/back camera toggle, flash, basic zoom
- Compose screen:
  - Add **caption**
  - `#hashtags` and `@mentions` are parsed live and rendered as clickable tokens **once posted** (and in the live preview)
  - Tapping a `#tag` → hashtag results screen; tapping `@mention` → that user's profile

---

### Tab 4 — User Profile

The signed-in user's own profile and account hub.

- **Profile header:** avatar, username, bio, followers/following counts, edit profile
- **Posts grid:** the user's own posts
- **Settings entry points:**
  - Account settings (email, password, linked Google)
  - **Theme** — light / dark / system
  - Notification preferences
  - Privacy
- **CMS pages** (server-driven content):
  - About
  - Terms of Service
  - Privacy Policy
  - Community Guidelines
- **Contact Us** (form → backend ticket)
- Logout

---

## 7. Cross-Cutting Modules

### Module 1 — Authentication
- Email + password sign up / sign in
- Google sign in
- Email OTP for verification + password reset
- JWT access + refresh token

### Module 2 — User Profile
- Username, bio, avatar
- Followers / following
- Verification badge (admin-granted)
- Trust score (Module 9)

### Module 3 — In-App Camera (Create flow)
- Photo + video capture
- Front/back, flash, zoom
- Embeds capture metadata (timestamp, device fingerprint, optional GPS) for later trust validation

### Module 4 — Media Upload & Processing
- Background upload queue with retry
- Image compression, video transcoding (HLS for streaming)
- Thumbnail generation
- Trust pipeline runs after successful upload

### Module 5 — Feed Service
- Personalized feed = (followed-user posts) ∪ (admin posts), ranked by trend/engagement
- Cursor-based pagination, Redis-cached per user
- Cold-start handling: 0-follow users see admin-only feed

### Module 6 — Engagement
- Like, comment (with replies), share, **save**
- `#hashtag` and `@mention` parsing and indexing
- Comment notifications

### Module 7 — Notifications
- Push via FCM (Android) / APNS (iOS)
- Triggers: like, comment, follow, mention, admin Fake-vs-Real post

### Module 8 — Messaging (Chat)
- Entry point: chat icon in top bar of Home
- 1-to-1 chat
- Text + media sharing
- Realtime via SignalR

### Module 9 — Trust Verification (USP)
Trust score computed from:
- Capture origin (in-app camera vs unknown)
- AI-detection confidence (Module 10)
- Metadata consistency
- Device trust
- User behavior signals

Output buckets: **Verified Real**, **Suspicious**, **AI Generated**.

### Module 10 — AI Detection Engine (Python FastAPI)
Detects:
- Deepfake faces / face swap
- Synthetic / AI-generated images
- Frame inconsistencies
- NSFW + violence (also feeds moderation)

Libraries: OpenCV, TensorFlow / PyTorch, InsightFace.

### Module 11 — Admin Panel (Web — `true_capture_admin_panel/`)
First-class operational console used by the platform team. Lives in its own Next.js 15 app, separate from the public web surface, and shares backend auth (a user with `IsAdmin=true` can sign in).

Responsibilities:
- **User moderation** — ban, unban, **restrict** (read-only / cannot post), **mute** (hidden from non-followers), **suspend** (cannot login for N days), verify badge, revoke badge
- **Content moderation** — hide (reversible), **remove** (hard-delete + audit), **veil as sensitive** (tap-to-reveal blur on mobile), Fake-vs-Real composer
- **Pre-publish approval queue** — policy-driven; certain posts (e.g., from new accounts) enter `pending_review` instead of going live; admin approves or rejects with reason
- **User analytics dashboard** — DAU/WAU, signups by day, posts and uploads by day, top creators by follower delta, Fake-vs-Real reach. MVP impl is read-only Postgres views; no event stream yet.
- **Broadcast / targeted notifications** — admin composes an announcement (title + body + optional CTA URL), picks audience (all users, by activity bucket — country filter deferred), stored in `admin_announcements`. Mobile polls until Phase 3 push lands.
- **Admin-to-user email send** — composer (subject + body) → single user or all users; dispatched via the same `IEmailSender` used for OTP; per-admin rate limit
- **Taxonomy / hashtag management** — banned hashtags (filtered from search, rejected at post-create) and featured hashtags (surface on explore later)
- **CMS page editor** — manages `cms_pages` (About, ToS, Privacy, Community Guidelines)
- **Contact Us inbox** — triage tickets through `open → in_progress → resolved | spam`
- **Data exports** — per-user JSON export of account + posts + media URLs + comments (support / GDPR-style requests)
- **Audit log** — every admin mutation writes a row to `admin_audit_log` (admin id, action, target, payload snapshot, timestamp); viewable in the panel
- **AI review queue** — placeholder UI in MVP; populated once the Phase 4 AI engine lands

### Module 12 — Content Moderation
Auto + manual review for: NSFW, violence, hate speech, spam.

### Module 13 — Search
- User search
- Hashtag search
- Trending tags
- Backed by Elasticsearch / OpenSearch

### Module 14 — Analytics
DAU, retention, watch time, upload success rate, trust-score distribution, Fake-vs-Real reach.

### Module 15 — Admin Console Infrastructure
Cross-cutting backing tables and services that power Module 11:
- `admin_audit_log` — append-only mutation history
- `admin_announcements` — broadcast / targeted announcements polled by the mobile app
- `banned_hashtags`, `featured_hashtags` — taxonomy controls
- `User.RestrictionLevel`, `User.RestrictionExpiresAtUtc` — restriction state
- `Post.Status` (`live | pending_review | rejected | removed`), `Post.Sensitive` — moderation state
- Reuses `IEmailSender` from Module 1 for admin-driven emails
- Depends on Notifications module (Module 7) for delivery once Phase 3 push lands; MVP uses DB-polled announcements

---

## 8. Suggested Backend Microservices

| Service | Responsibility |
|---|---|
| Auth | Login, tokens, Google OAuth |
| User | Profiles, follow graph |
| Media | Upload, transcoding, storage |
| Feed | Feed assembly + ranking |
| AI | Detection + trust scoring |
| Notification | Push delivery |
| Chat | 1-to-1 messaging (SignalR) |
| CMS | Static pages, Contact Us |
| Admin | Moderation, Fake-vs-Real publishing |
| Analytics | Event tracking |

---

## 9. Core Database Tables

- `users`
- `user_sessions`
- `follows`
- `posts` (with `is_admin_post`, `is_fake_vs_real` flags)
- `post_media`
- `post_hashtags`, `post_mentions`
- `comments`, `likes`, `saves`, `shares`
- `notifications`
- `messages`, `conversations`
- `trust_scores`
- `ai_scan_results`
- `cms_pages`
- `contact_messages`

---

## 10. Security Requirements

- JWT auth + refresh tokens
- Rate limiting on auth + upload endpoints
- Signed upload URLs
- Encrypted storage at rest
- CDN + DDoS protection (Cloudflare)
- PII handling for email, device fingerprint, optional GPS

---

## 11. Performance Goals

| Operation | Target |
|---|---|
| Feed load (cached) | < 2 s |
| Upload start | < 1 s |
| Video first frame | Instant |
| App cold start | < 3 s |
| AI trust scan | < 30 s |

---

## 12. Phase-wise Plan

### Phase 1 — MVP (8–12 weeks — expanded admin scope)
- Auth (email + Google)
- Profile
- In-app camera + post creation (photo, multi-photo, video)
- Feed (with admin-post baseline for new users)
- Fake vs Real tab (admin posts)
- Like / comment / share / save
- Profile tab with settings, theme, CMS pages, Contact Us
- **Operational admin panel** (separate `true_capture_admin_panel/` app):
  - Moderation (ban / restrict / mute / suspend / verify; hide / remove / sensitive-veil)
  - Fake-vs-Real composer
  - Pre-publish approval queue (policy-driven)
  - User analytics dashboard (Postgres-view-backed)
  - Broadcast / targeted announcements (DB-polled on mobile until push lands)
  - Admin-to-user email send
  - Taxonomy management (banned + featured hashtags)
  - CMS page editor + Contact inbox
  - Per-user data export
  - Audit log

> **Scope note:** Analytics (originally Phase 5) and broadcast notifications (originally Phase 3 delivery) are pulled forward into MVP via lightweight DB-polled implementations. Estimated MVP burn is 30–50% higher than the original "basic admin panel" target. Push (FCM/APNS), AI review, and event-stream analytics remain in their later phases.

### Phase 2 — Trust Infrastructure (4–6 weeks)
- Capture metadata + device fingerprinting
- Watermarking
- Trust scoring (rule-based v1)
- Verified creator badges

### Phase 3 — Engagement (5–6 weeks)
- Notifications (FCM/APNS)
- Chat (SignalR)
- Hashtag + user search
- Trending algorithm tuning

### Phase 4 — AI Intelligence (8–10 weeks)
- Deepfake detection
- AI-generated image detection
- Frame / voice analysis
- Moderation automation

### Phase 5 — Scale & Monetization (6–8 weeks)
- Creator monetization
- Verification subscription
- Ads
- Public analytics dashboard

---

## 13. Folder Structure Anchor

```
true-capture/
├── true_capture_app/          # Flutter mobile app
├── true_capture_backend/      # .NET 9 Web API
├── true_capture_web/          # Next.js 15 — public SEO + post pages (no admin)
├── true_capture_admin_panel/  # Next.js 15 — operational admin console
├── prd.md                     # this document
├── mobile_app_flow.md         # auto-updated when mobile modules change
├── backend_api_flow.md        # auto-updated when backend modules change
├── web_app_flow.md            # auto-updated when public web modules change
└── admin_panel_flow.md        # auto-updated when admin panel modules change
```

> Each `*_flow.md` doc is auto-maintained via a Claude Code PostToolUse hook at `.claude/hooks/flow_doc_reminder.sh`. When source files under a tracked surface are edited, the hook nudges the assistant to update the corresponding flow doc. Adding a new surface requires adding a case arm in that script.

---

## 14. Future Features

- Live streaming
- Blockchain-backed verification
- AR trust watermark
- News-agency verified channels
- Government / official verified channels
- Marketplace listing verification

---

## 15. Biggest Technical Risks

| Risk | Mitigation |
|---|---|
| Camera spoofing / screen replay | Behavior + sensor validation, watermarking |
| AI-detection false positives | Human moderation queue |
| Heavy video processing | Queue + worker architecture |
| Scalability | Microservices + CDN |
| Storage cost | Compression + tiered storage |

---

## 16. MVP Team

| Role | Count |
|---|---|
| Flutter Dev | 1 |
| Backend Dev (.NET) | 1 |
| Web Dev (Next.js) | 1 |
| AI Engineer | 1 |
| UI/UX Designer | 1 |
| DevOps | Part-time |

---

## 17. Success Metrics

### 3-month
- 10k registered users
- 100k uploads
- 70% week-1 retention
- 50+ Fake vs Real posts published

### 1-year
- Verified creator ecosystem
- Recognized public trust brand
- Fake vs Real cited by external media
