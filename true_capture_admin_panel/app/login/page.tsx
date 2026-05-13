"use client";

import { useActionState } from "react";
import { loginAction, type LoginState } from "./actions";

export default function LoginPage({
  searchParams,
}: {
  searchParams: { reason?: string; from?: string };
}) {
  const [state, formAction, pending] = useActionState<LoginState, FormData>(
    loginAction,
    {},
  );

  const reasonText =
    searchParams.reason === "not_admin"
      ? "Admin access required."
      : null;

  return (
    <main className="min-h-screen flex items-center justify-center px-6">
      <form
        action={formAction}
        className="w-full max-w-sm bg-white dark:bg-neutral-900 rounded-xl shadow p-8 space-y-4"
      >
        <h1 className="text-xl font-semibold">True Capture admin</h1>
        <p className="text-sm text-neutral-500">Sign in with an admin account.</p>

        {reasonText && (
          <div className="text-sm text-red-600 bg-red-50 dark:bg-red-950/40 p-2 rounded">
            {reasonText}
          </div>
        )}
        {state.error && (
          <div className="text-sm text-red-600 bg-red-50 dark:bg-red-950/40 p-2 rounded">
            {state.error}
          </div>
        )}

        <label className="block">
          <span className="text-sm">Email</span>
          <input
            name="email"
            type="email"
            required
            className="mt-1 w-full rounded border px-3 py-2 bg-transparent"
            autoComplete="username"
          />
        </label>

        <label className="block">
          <span className="text-sm">Password</span>
          <input
            name="password"
            type="password"
            required
            className="mt-1 w-full rounded border px-3 py-2 bg-transparent"
            autoComplete="current-password"
          />
        </label>

        <button
          type="submit"
          disabled={pending}
          className="w-full rounded bg-black text-white py-2 disabled:opacity-60"
        >
          {pending ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </main>
  );
}
