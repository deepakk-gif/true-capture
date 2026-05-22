"use server";

import { revalidatePath } from "next/cache";
import { serverFetch, ApiError } from "@/lib/api/server";

export type ReportState = { ok?: string; error?: string };

function describe(e: unknown): string {
  if (e instanceof ApiError) {
    if (e.status === 403) return "You do not have permission to moderate posts.";
    if (e.status === 404) return "Report or post not found.";
    if (e.status === 422) return e.errors.join(" ") || "Validation failed.";
    return e.errors[0] ?? e.message;
  }
  return "Unexpected error.";
}

/**
 * `PATCH /api/admin/post-reports/{id}` — resolve a report. The clicked submit
 * button supplies `action` (`dismiss | removePost | withholdAccount | sendNotice`).
 */
export async function resolveReportAction(
  reportId: number,
  _prev: ReportState,
  formData: FormData,
): Promise<ReportState> {
  const action = ((formData.get("action") as string | null) ?? "").trim();
  const reason = ((formData.get("reason") as string | null) ?? "").trim();

  if (!action) return { error: "Choose an action." };
  if (action === "sendNotice" && !reason) {
    return { error: "A message is required to send a notice." };
  }

  try {
    await serverFetch(`/api/admin/post-reports/${reportId}`, {
      method: "PATCH",
      body: JSON.stringify({ action, reason: reason.length > 0 ? reason : null }),
    });
    revalidatePath("/posts");
    return { ok: "Report resolved." };
  } catch (e) {
    return { error: describe(e) };
  }
}
