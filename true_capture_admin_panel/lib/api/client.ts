"use client";

// Browser fetch wrapper. The access cookie is HTTP-only (server-set), so we
// proxy through Next.js server actions for any call that needs the bearer.
// Anonymous calls (login) hit the backend directly via this helper.

const BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public errors: string[] = []
  ) {
    super(message);
  }
}

export async function clientPost<T>(path: string, body: unknown): Promise<T> {
  const res = await fetch(new URL(path, BASE_URL), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    let parsed: { errors?: string[] } = {};
    try {
      parsed = await res.json();
    } catch {
      /* ignore */
    }
    throw new ApiError(res.status, res.statusText, parsed.errors ?? []);
  }
  return (await res.json()) as T;
}
