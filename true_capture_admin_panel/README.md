# True Capture — Admin Panel

Operational admin console for True Capture. Lives in its own Next.js 15 app and
is **separate** from the public web surface at `../true_capture_web/`.

## Stack

- Next.js 15 (App Router, server components by default)
- TypeScript 5
- Tailwind CSS 3
- Auth: HTTP-only cookies populated by `POST /api/auth/login`; middleware 403s
  any signed-in user whose JWT `role` claim is not `Admin`

## Setup

```bash
pnpm install      # or npm install / yarn install
cp .env.example .env.local
# Set NEXT_PUBLIC_API_BASE_URL to your backend root (default http://localhost:5000)
pnpm dev          # → http://localhost:3000
```

## Layout

```
app/
├── (admin)/            # gated layout — sidebar + admin pages
│   ├── layout.tsx
│   └── users/page.tsx  # user list with filter
├── login/page.tsx
├── layout.tsx          # root layout
└── globals.css
components/
└── data-table.tsx      # shared list primitive
lib/api/
├── client.ts           # browser fetch wrapper
├── server.ts           # server-component fetch wrapper (reads cookies)
└── types.ts            # shared response types
middleware.ts           # auth gate
```

## Auth flow

1. `/login` form → `POST /api/auth/login` (backend on `NEXT_PUBLIC_API_BASE_URL`)
2. Tokens stored in HTTP-only cookies (`tc_access`, `tc_refresh`)
3. `middleware.ts` redirects unauthenticated callers to `/login` and 403s any
   user whose JWT lacks `role=Admin`
4. Admin pages live under the `(admin)` route group

See `../admin_panel_flow.md` for the per-module flow doc.
