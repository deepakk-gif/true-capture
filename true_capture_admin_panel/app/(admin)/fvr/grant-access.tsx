"use client";

import { useActionState } from "react";
import { grantFvrAccessAction, type FvrState } from "./actions";

/** Grant / revoke a candidate's Fake-vs-Real upload access. */
export function GrantAccess({ userId, granted }: { userId: number; granted: boolean }) {
  const bound = grantFvrAccessAction.bind(null, userId);
  const [state, formAction, pending] = useActionState<FvrState, FormData>(bound, {});

  return (
    <form action={formAction} className="flex items-center gap-2">
      <input type="hidden" name="granted" value={granted ? "false" : "true"} />
      <button
        type="submit"
        disabled={pending}
        className={`px-2 py-1 rounded text-xs border disabled:opacity-50 ${
          granted ? "border-red-300 text-red-600" : "border-green-400 text-green-700"
        }`}
      >
        {pending ? "…" : granted ? "Revoke access" : "Grant access"}
      </button>
      {state.error && <span className="text-xs text-red-600">{state.error}</span>}
      {state.ok    && <span className="text-xs text-green-600">{state.ok}</span>}
    </form>
  );
}
