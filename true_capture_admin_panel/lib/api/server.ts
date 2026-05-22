// Server-side fetch wrapper. Reads the access token out of the HTTP-only cookie
// and forwards it as a bearer header. Server components call this directly.

import { cookies } from "next/headers";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export const ACCESS_COOKIE = "tc_access";
export const REFRESH_COOKIE = "tc_refresh";

export class ApiError extends Error {
  constructor(public status: number, message: string, public errors: string[] = []) {
    super(message);
  }
}

export async function serverFetch<T>(
  path: string,
  init: RequestInit & { searchParams?: Record<string, string | number | boolean | undefined> } = {}
): Promise<T> {
  const cookieStore = await cookies();
  const access = cookieStore.get(ACCESS_COOKIE)?.value;

  const url = new URL(path, BASE_URL);
  for (const [k, v] of Object.entries(init.searchParams ?? {})) {
    if (v === undefined || v === null || v === "") continue;
    url.searchParams.set(k, String(v));
  }

  // For multipart uploads, let fetch set Content-Type (with the boundary) itself.
  const isFormData = init.body instanceof FormData;

  const res = await fetch(url, {
    ...init,
    headers: {
      ...(isFormData ? {} : { "Content-Type": "application/json" }),
      ...(access ? { Authorization: `Bearer ${access}` } : {}),
      ...(init.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!res.ok) {
    let body: { errors?: string[] } = {};
    try { body = await res.json(); } catch { /* ignore */ }
    throw new ApiError(res.status, res.statusText, body.errors ?? []);
  }

  return (await res.json()) as T;
}

/**
 * Resolves a possibly-relative backend media path (e.g. "/media/avatars/x.jpg")
 * into an absolute URL against the API base. Absolute URLs (S3/CDN) pass through.
 */
export function resolveMediaUrl(path: string | null | undefined): string | null {
  if (!path) return null;
  if (path.startsWith("http://") || path.startsWith("https://")) return path;
  return new URL(path, BASE_URL).toString();
}
