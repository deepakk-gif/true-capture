"use client";

import { useActionState } from "react";
import { resolveReportAction, type ReportState } from "./actions";

const ACTIONS: { value: string; label: string; danger?: boolean }[] = [
  { value: "dismiss",         label: "Dismiss" },
  { value: "removePost",      label: "Remove post",      danger: true },
  { value: "withholdAccount", label: "Withhold account", danger: true },
  { value: "sendNotice",      label: "Send notice" },
];

/** Per-report resolution form: a shared reason field + one button per action. */
export function ReportActions({ reportId, resolved }: { reportId: number; resolved: boolean }) {
  const bound = resolveReportAction.bind(null, reportId);
  const [state, formAction, pending] = useActionState<ReportState, FormData>(bound, {});

  if (resolved) {
    return <span className="text-xs text-neutral-500">Resolved</span>;
  }

  return (
    <form action={formAction} className="space-y-2 min-w-[260px]">
      <textarea
        name="reason"
        rows={2}
        placeholder="Reason / notice message (required for Send notice)"
        className="w-full rounded border px-2 py-1 text-xs bg-transparent"
      />
      <div className="flex flex-wrap gap-1">
        {ACTIONS.map((a) => (
          <button
            key={a.value}
            type="submit"
            name="action"
            value={a.value}
            disabled={pending}
            className={`px-2 py-1 rounded text-xs border disabled:opacity-50 ${
              a.danger ? "border-red-300 text-red-600" : "border-neutral-300"
            }`}
          >
            {a.label}
          </button>
        ))}
      </div>
      {state.ok    && <p className="text-xs text-green-600">{state.ok}</p>}
      {state.error && <p className="text-xs text-red-600">{state.error}</p>}
    </form>
  );
}
