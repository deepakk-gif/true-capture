"use client";

import { useActionState } from "react";
import { createFvrPostAction, type FvrState } from "./actions";

/** Composer for publishing an admin Fake-vs-Real post. */
export function FvrComposer() {
  const [state, formAction, pending] = useActionState<FvrState, FormData>(createFvrPostAction, {});

  return (
    <form action={formAction} className="space-y-4 rounded-xl border border-neutral-200 dark:border-neutral-800 p-4 bg-white dark:bg-neutral-900">
      <h2 className="font-semibold">Publish a Fake vs Real post</h2>

      <label className="flex flex-col text-sm">
        <span className="text-neutral-500 mb-1">Caption (required)</span>
        <textarea
          name="caption"
          rows={3}
          required
          placeholder="Explain what this post shows…"
          className="rounded border px-2 py-1.5 bg-transparent"
        />
      </label>

      <label className="flex flex-col text-sm">
        <span className="text-neutral-500 mb-1">Photos / videos (required — images or videos, no GIFs)</span>
        <input
          type="file"
          name="files"
          multiple
          accept="image/jpeg,image/png,image/webp,video/mp4,video/quicktime"
          required
          className="text-sm"
        />
      </label>

      <fieldset className="space-y-2">
        <legend className="text-sm text-neutral-500 mb-1">Reference links (at least one required)</legend>
        {[0, 1, 2].map((i) => (
          <input
            key={i}
            type="url"
            name="reference"
            placeholder="https://example.com/source"
            className="w-full rounded border px-2 py-1.5 bg-transparent text-sm"
          />
        ))}
      </fieldset>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={pending}
          className="px-4 py-2 rounded bg-black text-white text-sm disabled:opacity-50"
        >
          {pending ? "Publishing…" : "Publish"}
        </button>
        {state.ok    && <span className="text-sm text-green-600">{state.ok}</span>}
        {state.error && <span className="text-sm text-red-600">{state.error}</span>}
      </div>
    </form>
  );
}
