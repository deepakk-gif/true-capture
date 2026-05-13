"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { decodeJwtClaims } from "@/lib/jwt";
import { ACCESS_COOKIE, REFRESH_COOKIE } from "@/lib/api/server";
import type { AuthTokens } from "@/lib/api/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export type LoginState = { error?: string };

export async function loginAction(
  _prev: LoginState,
  formData: FormData,
): Promise<LoginState> {
  const email    = (formData.get("email")    as string | null)?.trim() ?? "";
  const password = (formData.get("password") as string | null) ?? "";

  if (!email || !password) {
    return { error: "Email and password are required." };
  }

  const res = await fetch(new URL("/api/auth/login", BASE_URL), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    return { error: res.status === 401 ? "Invalid credentials." : "Sign-in failed." };
  }

  const tokens = (await res.json()) as AuthTokens;
  const claims = decodeJwtClaims(tokens.accessToken);

  if (claims?.role !== "Admin") {
    return { error: "This account does not have admin access." };
  }

  const jar = await cookies();
  const accessExp = new Date(tokens.accessExpiresAtUtc);
  jar.set(ACCESS_COOKIE, tokens.accessToken, {
    httpOnly: true,
    secure:   process.env.NODE_ENV === "production",
    sameSite: "lax",
    path:     "/",
    expires:  accessExp,
  });
  jar.set(REFRESH_COOKIE, tokens.refreshToken, {
    httpOnly: true,
    secure:   process.env.NODE_ENV === "production",
    sameSite: "lax",
    path:     "/",
    maxAge:   60 * 60 * 24 * 30,    // 30 days, matches backend default
  });

  redirect("/users");
}

export async function logoutAction(): Promise<void> {
  const jar = await cookies();
  jar.delete(ACCESS_COOKIE);
  jar.delete(REFRESH_COOKIE);
  redirect("/login");
}
