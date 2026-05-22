# Admin Panel Flow — `true_capture_admin_panel` (Next.js 15)

> Auto-maintained doc. Captures the route + data flow per module:
> Page (server component) → Server Action / fetch wrapper → Backend `/api/admin/*` → DB.
> Update this file whenever a module changes.

## Architecture (shared)

- **Framework**: Next.js 15 (App Router, TypeScript). Server components by default; mark interactive surfaces with `"use client"`.
- **Auth**: shares the backend's `/api/auth/login` flow. Access + refresh tokens stored in HTTP-only cookies (`tc_access`, `tc_refresh`). A middleware at `middleware.ts` redirects unauthenticated callers to `/login` and returns `403` for any user whose JWT claim set lacks `role=Admin`.
- **Fetch wrapper**: `lib/api/server.ts` (server-side, reads `tc_access` cookie + attaches bearer; throws `ApiError` on non-2xx) and `lib/api/client.ts` (anonymous calls only — login). Both prepend `process.env.NEXT_PUBLIC_API_BASE_URL`.
- **Layout**: `app/(admin)/layout.tsx` provides the sidebar nav (Users, **New admin** [super-admin only], Posts, Approval Queue, Fake vs Real, Analytics, Announcements, Email, Taxonomy, CMS, Contact, Audit Log) and a logout form that calls `logoutAction` server-action. The sidebar is an async server component that reads the `tc_access` cookie and filters `NAV` entries by `hasPermission(token, item.requires)` — e.g., **New admin** only appears for users carrying `Users.CreateAdmin`.
- **Audit**: every successful admin mutation triggers a server-side audit log write through the backend; UI surfaces are read-only against `admin_audit_log` via `GET /api/admin/audit` (TBD).
- **Style**: Tailwind CSS 3 + a single shared `DataTable` primitive at `components/data-table.tsx` reused across every list page. `StatusBadge` helper for boolean flag chips.

## Bootstrap

| File | Purpose |
|---|---|
| `package.json` | next 15.0.3, react 19 RC, tailwind 3.4, typescript 5.6, eslint-config-next |
| `tsconfig.json` | strict TS, `@/*` path alias |
| `next.config.mjs` | `reactStrictMode`, `experimental.typedRoutes` on |
| `tailwind.config.ts` | scans `./app/**` and `./components/**` |
| `postcss.config.mjs` | tailwind + autoprefixer |
| `app/globals.css` | Tailwind base/components/utilities + light/dark body palette |
| `app/layout.tsx` | root layout — `<html><body>{children}</body></html>` |
| `.env.example` | `NEXT_PUBLIC_API_BASE_URL=http://localhost:5000` |
| `README.md` | setup + dev instructions |

## Auth gate

### Files
| Path | Role |
|---|---|
| `middleware.ts` | edge middleware — checks `tc_access` cookie; redirects to `/login` on missing; redirects with `?reason=not_admin` and clears cookies when JWT role ≠ Admin |
| `lib/jwt.ts` | `decodeJwtClaims(token)` + `isAdminToken(token)` + `permissionsFromToken(token)` + `hasPermission(token, code)` — base64url-only, no crypto verify (backend is authority). The `perms` claim is a comma-separated permission-code list set by `TokenService.Issue`; `permissionsFromToken` splits + trims it. |
| `lib/api/server.ts` | `serverFetch<T>` server-component fetch wrapper; exports `ACCESS_COOKIE`/`REFRESH_COOKIE` constants and `ApiError` |
| `lib/api/client.ts` | `clientPost<T>` browser fetch (anonymous — used only by login server action's downstream calls) |
| `app/login/page.tsx` | client component with `useActionState(loginAction)` — email/password form + error display |
| `app/login/actions.ts` | server actions `loginAction` and `logoutAction` |

### Login flow
1. User opens `/login` (only public route under the middleware matcher).
2. Form submits to `loginAction` (server action) with `{ email, password }`.
3. Server action `POST`s `/api/auth/login` against `NEXT_PUBLIC_API_BASE_URL`.
4. Decodes the returned JWT; rejects with `error: "This account does not have admin access."` if `role != "Admin"`.
5. Sets HTTP-only cookies:
   - `tc_access` — expires at `accessExpiresAtUtc`.
   - `tc_refresh` — 30-day max-age (matches backend default).
6. `redirect("/users")` lands the admin on the first usable page.

### Logout flow
- Sidebar `Log out` button submits a `<form action={logoutAction}>` server action that deletes both cookies and `redirect("/login")`.

## Users Module — DONE

### Endpoint consumed
`GET /api/admin/users?search=&isActive=&isAdmin=&isVerified=&hasGoogle=&cursor=&limit=20` → `AdminUserListResult { items, nextCursor?, total }`.

### Files
| Path | Role |
|---|---|
| `app/(admin)/layout.tsx` | gated sidebar shell |
| `app/(admin)/page.tsx` | redirects `/` → `/users` for signed-in admins |
| `app/(admin)/users/page.tsx` | async server component — reads `searchParams`, calls `serverFetch`, renders filter bar + `DataTable` + pagination links |
| `components/data-table.tsx` | shared `DataTable<T>` + `StatusBadge` |
| `components/sidebar.tsx` | static nav + logout form |
| `lib/api/types.ts` | `AdminUserListItem`, `AdminUserListResult`, `AdminUserListQuery` mirroring the backend record shapes |

### Flow
1. Browser navigates to `/users?search=&isActive=true&...`.
2. `app/(admin)/users/page.tsx` is a server component. It reads `searchParams` (typed as `Promise<RawSearchParams>` per Next 15 App Router) and normalizes them via `buildQuery`:
   - `search` → trimmed string.
   - Tri-state booleans (`isActive`, `isAdmin`, `isVerified`, `hasGoogle`) parsed via `tribool("true"|"false"|undefined)`.
   - `cursor` passed through verbatim.
   - `limit` pinned to 20.
3. Calls `serverFetch<AdminUserListResult>("/api/admin/users", { searchParams: q })`, which:
   - Reads `tc_access` from `cookies()`.
   - Appends bearer header.
   - Skips undefined / empty query params.
   - Throws `ApiError` on non-2xx (caught by the global error boundary).
4. Renders:
   - **Header**: total count.
   - **FilterBar** (server-rendered `<form method="GET">`): search input + four tri-state selects + Apply / Reset buttons. Submit reloads the page with the new query string — no client JS.
   - **DataTable** with four columns: `User` (display name + @username + email), `Status` (Active/Banned + Admin/Verified/Google/Email-unverified badges), `Last login`, `Joined`.
   - **Pagination**: when `cursor` is set in URL, show "First page" link to clear it; when `nextCursor` is returned, show "Next →" that preserves all filters and adds `?cursor=<nextCursor>`.

### Auth call-chain summary
Browser `/users?…` → `middleware.ts` (gate) → server component → `serverFetch` (reads cookie) → backend `[AdminOnly]` filter on `/api/admin/users` → `AdminUsersService.ListAsync` → Postgres → JSON → table rows.

## New Admin (super-admin only) — DONE

Mints a new admin account with an explicit per-user permission set. Visible only to users carrying `Users.CreateAdmin` (see sidebar gating above).

### Files
| Path | Role |
|---|---|
| `app/(admin)/users/new/page.tsx` | Server component. Reads `tc_access` cookie; `redirect("/users")` if `hasPermission(access, "Users.CreateAdmin")` is false (defence-in-depth in case the link leaks). Fetches `/api/admin/permissions` via `serverFetch`, groups by `module`, passes to the form. |
| `app/(admin)/users/new/new-admin-form.tsx` | Client component (`"use client"`). Uses `useActionState(createAdminAction)`. Renders email / username / displayName / initial password inputs + one `<fieldset>` per permission module with a checkbox per code. Inline success/error banners reflect the action state. |
| `app/(admin)/users/new/actions.ts` | Server action `createAdminAction(prev, formData)`: pulls fields + every `permissionCodes` value, POSTs to `/api/admin/users`, maps `ApiError` to a friendly message (`403` → "no permission", `409` → conflict, `422` → validation), and calls `revalidatePath("/users")` so the new admin shows up in the listing on next navigation. |

### Flow
1. Super-admin clicks **New admin** in the sidebar (link is rendered only when their JWT `perms` claim contains `Users.CreateAdmin`).
2. Browser hits `/users/new` → server component double-checks permission, fetches `GET /api/admin/permissions` (gated by `[RequirePermission("Users.CreateAdmin")]`), groups results by `module`, renders the form.
3. User submits → `createAdminAction` server-action posts `{ email, username, password, displayName?, permissionCodes[] }` to `POST /api/admin/users`.
4. Backend `AdminAccountsService.CreateAdminAsync` validates, creates the `User` with `IsAdmin=true`, attaches the `admin` role, inserts one `UserPermission` per requested code, returns `CreatedAdminResponse`.
5. Action returns `{ ok: true, granted: codes }` to the form — banner shows "Admin created. Granted permissions: …".
6. `revalidatePath("/users")` invalidates the user-list cache so the freshly-minted admin appears immediately the next time the super-admin navigates back.

## User Detail / Management — DONE

Per-user management page reached by clicking any row on `/users`. Lets an admin edit a user's profile (display name + bio), manage their avatar, and activate/suspend them. All mutations require the `Users.Manage` permission.

### Endpoints consumed
| Method | Route | Purpose |
|---|---|---|
| GET    | `/api/admin/users/{id}`        | `AdminUserDetail` for the page |
| PUT    | `/api/admin/users/{id}`        | edit display name + bio |
| POST   | `/api/admin/users/{id}/avatar` | upload avatar (multipart `file`) |
| DELETE | `/api/admin/users/{id}/avatar` | remove avatar |
| POST   | `/api/admin/users/{id}/status` | activate / suspend (`{ isActive }`) |

### Files
| Path | Role |
|---|---|
| `app/(admin)/users/[id]/page.tsx` | Server component. `serverFetch<AdminUserDetail>` for the user; resolves the avatar URL with `resolveMediaUrl` (done server-side so the client component need not import `lib/api/server.ts`); reads `Users.Manage` from the JWT via `hasPermission` → `canManage`. Shows a read-only banner when `canManage` is false. |
| `app/(admin)/users/[id]/user-detail-form.tsx` | Client component `UserDetailView`. Four `useActionState` forms (profile / avatar upload / avatar remove / status). Each server action is bound to the user id via `.bind(null, id)`. Avatar shown via plain `<img>` (or initials fallback); read-only account-info grid. |
| `app/(admin)/users/[id]/actions.ts` | Server actions `updateUserAction`, `uploadAvatarAction`, `removeAvatarAction`, `setStatusAction` — each `(id, prev, formData)`. Forward to the backend via `serverFetch`, `revalidatePath('/users/{id}')` on success, map `ApiError` (403/404/422) to friendly text. |
| `lib/api/types.ts` | `AdminUserDetail` type added (mirrors the backend DTO — adds `bio`). |
| `lib/api/server.ts` | `serverFetch` now omits the JSON `Content-Type` when the body is `FormData` (lets fetch set the multipart boundary); new `resolveMediaUrl(path)` resolves relative backend media paths to absolute URLs. |
| `app/(admin)/users/page.tsx` | The `User` cell is now a `<Link href="/users/{id}">` into this page. |

### Flow
1. Admin clicks a user row on `/users` → `/users/{id}`.
2. `page.tsx` (server) fetches `GET /api/admin/users/{id}`, resolves the avatar URL, computes `canManage`, renders `UserDetailView`.
3. Each section is its own `<form action={boundServerAction}>`:
   - **Profile** → `updateUserAction` → `PUT /api/admin/users/{id}`.
   - **Avatar upload** → `uploadAvatarAction` (multipart) → `POST /api/admin/users/{id}/avatar`.
   - **Remove avatar** → `removeAvatarAction` → `DELETE /api/admin/users/{id}/avatar`.
   - **Suspend/Activate** → `setStatusAction` (hidden `isActive` field) → `POST /api/admin/users/{id}/status`.
4. On success the action `revalidatePath`s the detail route so the page re-renders with fresh server data; the form's `useActionState` shows an inline success/error banner.

### Auth call-chain summary
Browser `/users/{id}` → `middleware.ts` (admin gate) → server component → `serverFetch` → backend `[AdminOnly]` → `AdminUsersController` → `AdminUsersService` / `UserProfileService` → Postgres + `IFileStorage`. Mutations additionally pass `[RequirePermission("Users.Manage")]`; a plain admin without that grant sees the read-only banner and gets a `403`-mapped message if they somehow submit.

### Extended — social report, posts moderation, per-user messaging
The detail page now also surfaces the social graph and lets an admin message the user (privacy is bypassed — the admin sees everything regardless of public/private).

| Method | Route | Purpose |
|---|---|---|
| GET    | `/api/admin/users/{id}/posts` | the user's posts (moderation grid) |
| DELETE | `/api/admin/posts/{postId}`   | delete a post (needs `Posts.Moderate`) |
| POST   | `/api/admin/users/{id}/notify` | send an FCM push |
| POST   | `/api/admin/users/{id}/notice` | create an in-app notice |
| POST   | `/api/admin/users/{id}/email`  | email the user |

- `lib/api/types.ts` — `AdminUserDetail` gains `accountType` + `followersCount`/`followingCount`/`postsCount`; new `AdminPost` / `AdminPostList`.
- `page.tsx` — additionally `serverFetch`es `GET /api/admin/users/{id}/posts` and resolves each post image URL server-side; passes `posts` to the view.
- `user-detail-form.tsx` — adds a follower/following/post **stat row**, a **posts thumbnail grid** (`PostCell`, each with its own `useActionState` delete bound to `deletePostAction(userId, postId)`), and a **"Message this user"** section with three forms — push / in-app notice / email — each a `useActionState`-backed `MessageForm`.
- `actions.ts` — new server actions `notifyUserAction`, `noticeUserAction`, `emailUserAction`, `deletePostAction`.

## Create Post Module — DONE (moderation queue + Fake-vs-Real hub)

Two pages, both server components reading via `serverFetch`; mutations are server actions
returning `{ ok?, error? }` consumed by client components with `useActionState`.

### Post moderation — `/posts`
- `app/(admin)/posts/page.tsx` — server component, `serverFetch`es
  `GET /api/admin/post-reports?status=&cursor=` (`PostReportList`), renders a `DataTable`;
  status pills (`open` / `resolved` / `all`) reload the page; keyset pagination.
- `report-actions.tsx` (client) — per-row resolution form: a shared `reason` textarea +
  four submit buttons (`dismiss` / `removePost` / `withholdAccount` / `sendNotice`); the
  clicked button's `value` is the action.
- `actions.ts` — `resolveReportAction(reportId, prev, formData)` →
  `PATCH /api/admin/post-reports/{id}` `{ action, reason }`.

### Fake vs Real — `/fvr`
- `app/(admin)/fvr/page.tsx` — server component: renders the composer + an upload-access
  candidate `DataTable` from `GET /api/admin/fvr-candidates` (`FvrCandidateList`).
- `composer.tsx` (client) — caption + multi-file input + three reference-link inputs →
  `createFvrPostAction`.
- `grant-access.tsx` (client) — Grant / Revoke button → `grantFvrAccessAction`.
- `actions.ts`:
  - `createFvrPostAction(prev, formData)` — runs the **signed-URL media pipeline
    server-side** for every file (`POST /api/media/uploads` → `PUT /api/media/blob/{id}`
    raw bytes → `POST /api/media/finalize`), then `POST /api/admin/posts`
    `{ mediaAssetIds, caption, references }`.
  - `grantFvrAccessAction(userId, prev, formData)` →
    `POST /api/admin/users/{id}/fake-vs-real-access` `{ granted }`.

`lib/api/types.ts` — `AdminPost` updated (`coverUrl`/`type`/`kind`, was `imageUrl`); new
`PostReport`/`PostReportList`, `FvrCandidate`/`FvrCandidateList`. Sidebar `/posts` + `/fvr`
nav entries already existed. The signed-URL pipeline is driven from the **server action**
because the admin auth token lives in an HTTP-only cookie the browser cannot read.

## Pending Modules

To be filled in as they land:

- Pre-publish approval queue (task 11.9-13)
- Analytics dashboard (task 11.15-17)
- Broadcast announcements composer (task 11.18-21)
- Admin-to-user email composer (task 11.22-24)
- Taxonomy management (task 11.25-27)
- Data exports (task 11.28-29)
- Audit log viewer (task 11.30-32)
- CMS editor (task 11.33)
- Contact inbox (task 11.34)
