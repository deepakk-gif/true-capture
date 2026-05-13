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

True Capture ships on three surfaces. Each lives in its own folder:

| Surface | Folder | Purpose |
|---|---|---|
| Mobile app | `true_capture_app/` | Primary consumer experience (Flutter, iOS + Android) |
| Backend API | `true_capture_backend/` | .NET 9 Web API serving all clients |
| Web platform | `true_capture_web/` | Next.js 15 — SEO landing, public post pages, admin panel |

Module/flow change tracking lives in:
- `mobile_app_flow.md`
- `backend_api_flow.md`
- `web_app_flow.md`

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

### Module 11 — Admin Panel (Web)
- User moderation, post moderation, ban system
- AI review queue
- **Fake vs Real composer** — first-class workflow for the editorial team to publish to Tab 2
- CMS page editor (powers Profile → CMS pages)
- Contact Us inbox

### Module 12 — Content Moderation
Auto + manual review for: NSFW, violence, hate speech, spam.

### Module 13 — Search
- User search
- Hashtag search
- Trending tags
- Backed by Elasticsearch / OpenSearch

### Module 14 — Analytics
DAU, retention, watch time, upload success rate, trust-score distribution, Fake-vs-Real reach.

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

### Phase 1 — MVP (6–8 weeks)
- Auth (email + Google)
- Profile
- In-app camera + post creation (photo, multi-photo, video)
- Feed (with admin-post baseline for new users)
- Fake vs Real tab (admin posts)
- Like / comment / share / save
- Profile tab with settings, theme, CMS pages, Contact Us
- Basic admin panel (user mod + Fake-vs-Real composer)

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
├── true_capture_app/        # Flutter mobile app
├── true_capture_backend/    # .NET 9 Web API
├── true_capture_web/        # Next.js 15 web + admin
├── prd.md                   # this document
├── mobile_app_flow.md       # auto-updated when mobile modules change
├── backend_api_flow.md      # auto-updated when backend modules change
└── web_app_flow.md          # auto-updated when web modules change
```

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
