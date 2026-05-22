"use server";

import { revalidatePath } from "next/cache";
import { serverFetch, ApiError } from "@/lib/api/server";

export type DetailState = { ok?: string; error?: string };

function describe(e: unknown): string {
  if (e instanceof ApiError) {
    if (e.status === 403) return "You do not have permission to manage users.";
    if (e.status === 404) return "User not found.";
    if (e.status === 422) return e.errors.join(" ") || "Validation failed.";
    return e.errors[0] ?? e.message;
  }
  return "Unexpected error.";
}

/** `PUT /api/admin/users/{id}` — edit display name + bio. */
export async function updateUserAction(
  id: string,
  _prev: DetailState,
  formData: FormData,
): Promise<DetailState> {
  const displayName = ((formData.get("displayName") as string | null) ?? "").trim();
  const bio         = ((formData.get("bio")         as string | null) ?? "").trim();

  try {
    await serverFetch(`/api/admin/users/${id}`, {
      method: "PUT",
      body: JSON.stringify({
        displayName: displayName.length > 0 ? displayName : null,
        bio:         bio.length > 0 ? bio : null,
      }),
    });
    revalidatePath(`/users/${id}`);
    return { ok: "Profile updated." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `POST /api/admin/users/{id}/avatar` — upload a new avatar image (multipart). */
export async function uploadAvatarAction(
  id: string,
  _prev: DetailState,
  formData: FormData,
): Promise<DetailState> {
  const file = formData.get("file");
  if (!(file instanceof File) || file.size === 0) {
    return { error: "Choose an image to upload." };
  }

  const forward = new FormData();
  forward.append("file", file);

  try {
    await serverFetch(`/api/admin/users/${id}/avatar`, { method: "POST", body: forward });
    revalidatePath(`/users/${id}`);
    return { ok: "Avatar updated." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `DELETE /api/admin/users/{id}/avatar` — clear the avatar. */
export async function removeAvatarAction(
  id: string,
  _prev: DetailState,
  _formData: FormData,
): Promise<DetailState> {
  try {
    await serverFetch(`/api/admin/users/${id}/avatar`, { method: "DELETE" });
    revalidatePath(`/users/${id}`);
    return { ok: "Avatar removed." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `POST /api/admin/users/{id}/status` — activate / suspend the user. */
export async function setStatusAction(
  id: string,
  _prev: DetailState,
  formData: FormData,
): Promise<DetailState> {
  const isActive = formData.get("isActive") === "true";

  try {
    await serverFetch(`/api/admin/users/${id}/status`, {
      method: "POST",
      body: JSON.stringify({ isActive }),
    });
    revalidatePath(`/users/${id}`);
    return { ok: isActive ? "User activated." : "User suspended." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `POST /api/admin/users/{id}/notify` — send an FCM push to the user. */
export async function notifyUserAction(
  id: string,
  _prev: DetailState,
  formData: FormData,
): Promise<DetailState> {
  const title = ((formData.get("title") as string | null) ?? "").trim();
  const body  = ((formData.get("body")  as string | null) ?? "").trim();
  if (!title || !body) return { error: "Title and body are required." };

  try {
    await serverFetch(`/api/admin/users/${id}/notify`, {
      method: "POST",
      body: JSON.stringify({ title, body }),
    });
    return { ok: "Push notification sent." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `POST /api/admin/users/{id}/notice` — create an in-app notice for the user. */
export async function noticeUserAction(
  id: string,
  _prev: DetailState,
  formData: FormData,
): Promise<DetailState> {
  const title = ((formData.get("title") as string | null) ?? "").trim();
  const body  = ((formData.get("body")  as string | null) ?? "").trim();
  if (!title || !body) return { error: "Title and body are required." };

  try {
    await serverFetch(`/api/admin/users/${id}/notice`, {
      method: "POST",
      body: JSON.stringify({ title, body }),
    });
    return { ok: "In-app notice sent." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `POST /api/admin/users/{id}/email` — email the user. */
export async function emailUserAction(
  id: string,
  _prev: DetailState,
  formData: FormData,
): Promise<DetailState> {
  const subject = ((formData.get("subject") as string | null) ?? "").trim();
  const body    = ((formData.get("body")    as string | null) ?? "").trim();
  if (!subject || !body) return { error: "Subject and body are required." };

  try {
    await serverFetch(`/api/admin/users/${id}/email`, {
      method: "POST",
      body: JSON.stringify({ subject, body }),
    });
    return { ok: "Email sent." };
  } catch (e) {
    return { error: describe(e) };
  }
}

/** `DELETE /api/admin/posts/{postId}` — remove a post (moderation). */
export async function deletePostAction(
  userId: string,
  postId: number,
  _prev: DetailState,
  _formData: FormData,
): Promise<DetailState> {
  try {
    await serverFetch(`/api/admin/posts/${postId}`, { method: "DELETE" });
    revalidatePath(`/users/${userId}`);
    return { ok: "Post deleted." };
  } catch (e) {
    return { error: describe(e) };
  }
}
