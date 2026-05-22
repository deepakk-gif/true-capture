"use server";

import { revalidatePath } from "next/cache";
import { serverFetch, ApiError } from "@/lib/api/server";

export type FvrState = { ok?: string; error?: string };

function describe(e: unknown): string {
  if (e instanceof ApiError) {
    if (e.status === 403) return "You do not have permission to publish Fake vs Real posts.";
    if (e.status === 413) return "A file exceeds the upload size limit.";
    if (e.status === 422) return e.errors.join(" ") || "Validation failed.";
    return e.errors[0] ?? e.message;
  }
  return "Unexpected error.";
}

type UploadTicket = { uploadId: number; putUrl: string; expiresAtUtc: string };

/**
 * `POST /api/admin/posts` — publish a Fake-vs-Real post. Runs the signed-URL media
 * pipeline server-side for every chosen file (uploads → PUT bytes → finalize), then
 * creates the post with the resulting media ids + reference links.
 */
export async function createFvrPostAction(
  _prev: FvrState,
  formData: FormData,
): Promise<FvrState> {
  const caption    = ((formData.get("caption") as string | null) ?? "").trim();
  const files      = formData.getAll("files").filter((f): f is File => f instanceof File && f.size > 0);
  const references = formData.getAll("reference")
    .map((r) => String(r).trim())
    .filter((r) => r.length > 0);

  if (!caption)            return { error: "A caption is required." };
  if (files.length === 0)  return { error: "Add at least one photo or video." };
  if (references.length === 0) return { error: "Add at least one reference link." };

  try {
    const mediaAssetIds: number[] = [];
    for (const file of files) {
      const kind = file.type.startsWith("video/") ? "video" : "photo";

      const ticket = await serverFetch<UploadTicket>("/api/media/uploads", {
        method: "POST",
        body: JSON.stringify({ mimeType: file.type, byteSize: file.size, kind }),
      });

      const bytes = Buffer.from(await file.arrayBuffer());
      await serverFetch(`/api/media/blob/${ticket.uploadId}`, {
        method: "PUT",
        headers: { "Content-Type": "application/octet-stream" },
        body: bytes,
      });

      await serverFetch("/api/media/finalize", {
        method: "POST",
        body: JSON.stringify({ uploadId: ticket.uploadId, captureMetadata: null }),
      });

      mediaAssetIds.push(ticket.uploadId);
    }

    await serverFetch("/api/admin/posts", {
      method: "POST",
      body: JSON.stringify({ mediaAssetIds, caption, references }),
    });

    revalidatePath("/fvr");
    return { ok: "Fake vs Real post published." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `POST /api/admin/users/{id}/fake-vs-real-access` — grant or revoke upload access. */
export async function grantFvrAccessAction(
  userId: number,
  _prev: FvrState,
  formData: FormData,
): Promise<FvrState> {
  const granted = formData.get("granted") === "true";
  try {
    await serverFetch(`/api/admin/users/${userId}/fake-vs-real-access`, {
      method: "POST",
      body: JSON.stringify({ granted }),
    });
    revalidatePath("/fvr");
    return { ok: granted ? "Access granted." : "Access revoked." };
  } catch (e) {
    return { error: describe(e) };
  }
}
