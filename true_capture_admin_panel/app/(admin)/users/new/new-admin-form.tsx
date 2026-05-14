"use client";

import { useActionState } from "react";
import { createAdminAction, type CreateAdminState } from "./actions";

type PermissionGroup = {
  module: string;
  perms: { code: string; description: string | null }[];
};

export function NewAdminForm({
  permissionGroups,
}: {
  permissionGroups: PermissionGroup[];
}) {
  const [state, formAction, pending] = useActionState<CreateAdminState, FormData>(
    createAdminAction,
    {},
  );

  return (
    <form
      action={formAction}
      className="space-y-6 max-w-2xl bg-white dark:bg-neutral-900 rounded-xl border border-neutral-200 dark:border-neutral-800 p-6"
    >
      {state.error && (
        <div className="text-sm text-red-600 bg-red-50 dark:bg-red-950/40 p-3 rounded">
          {state.error}
        </div>
      )}
      {state.ok && (
        <div className="text-sm text-emerald-700 bg-emerald-50 dark:bg-emerald-950/40 p-3 rounded">
          Admin created.
          {state.granted && state.granted.length > 0 && (
            <> Granted permissions: <code>{state.granted.join(", ")}</code></>
          )}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <label className="block">
          <span className="text-sm text-neutral-500">Email</span>
          <input
            name="email"
            type="email"
            required
            className="mt-1 w-full rounded border px-3 py-2 bg-transparent"
            autoComplete="off"
          />
        </label>

        <label className="block">
          <span className="text-sm text-neutral-500">Username</span>
          <input
            name="username"
            type="text"
            required
            minLength={3}
            className="mt-1 w-full rounded border px-3 py-2 bg-transparent"
            autoComplete="off"
          />
        </label>

        <label className="block">
          <span className="text-sm text-neutral-500">Display name (optional)</span>
          <input
            name="displayName"
            type="text"
            className="mt-1 w-full rounded border px-3 py-2 bg-transparent"
            autoComplete="off"
          />
        </label>

        <label className="block">
          <span className="text-sm text-neutral-500">Initial password</span>
          <input
            name="password"
            type="password"
            required
            minLength={8}
            className="mt-1 w-full rounded border px-3 py-2 bg-transparent"
            autoComplete="new-password"
          />
        </label>
      </div>

      <div>
        <h2 className="text-sm font-semibold mb-2">Permissions</h2>
        <p className="text-xs text-neutral-500 mb-3">
          Pick the powers this admin will have. Anything not checked is denied.
        </p>
        <div className="space-y-4">
          {permissionGroups.map((group) => (
            <fieldset
              key={group.module}
              className="border border-neutral-200 dark:border-neutral-800 rounded p-3"
            >
              <legend className="px-2 text-xs font-semibold text-neutral-500">
                {group.module}
              </legend>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {group.perms.map((p) => (
                  <label key={p.code} className="flex items-start gap-2 text-sm">
                    <input
                      type="checkbox"
                      name="permissionCodes"
                      value={p.code}
                      className="mt-1"
                    />
                    <span>
                      <code className="font-mono text-xs">{p.code}</code>
                      {p.description && (
                        <span className="block text-xs text-neutral-500">
                          {p.description}
                        </span>
                      )}
                    </span>
                  </label>
                ))}
              </div>
            </fieldset>
          ))}
        </div>
      </div>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={pending}
          className="px-4 py-2 rounded bg-black text-white disabled:opacity-60"
        >
          {pending ? "Creating…" : "Create admin"}
        </button>
      </div>
    </form>
  );
}
