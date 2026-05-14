"use server";

import { revalidatePath } from "next/cache";
import { serverFetch, ApiError } from "@/lib/api/server";

export type CreateAdminState = {
  ok?:    boolean;
  error?: string;
  granted?: string[];
};

type CreatedAdminResponse = {
  id: number;
  email: string;
  username: string;
  displayName: string | null;
  grantedPermissionCodes: string[];
};

export async function createAdminAction(
  _prev: CreateAdminState,
  formData: FormData,
): Promise<CreateAdminState> {
  const email           = ((formData.get("email")       as string | null) ?? "").trim();
  const username        = ((formData.get("username")    as string | null) ?? "").trim();
  const password        =  (formData.get("password")    as string | null) ?? "";
  const displayName     = ((formData.get("displayName") as string | null) ?? "").trim();
  const permissionCodes = formData.getAll("permissionCodes").map((v) => String(v));

  if (!email || !username || !password) {
    return { error: "Email, username, and password are required." };
  }
  if (password.length < 8) {
    return { error: "Password must be at least 8 characters." };
  }

  try {
    const created = await serverFetch<CreatedAdminResponse>("/api/admin/users", {
      method: "POST",
      body: JSON.stringify({
        email,
        username,
        password,
        displayName:     displayName.length > 0 ? displayName : null,
        permissionCodes,
      }),
    });

    // Refresh the listing so the new admin shows up immediately.
    revalidatePath("/users");

    return { ok: true, granted: created.grantedPermissionCodes };
  } catch (e) {
    if (e instanceof ApiError) {
      if (e.status === 403)  return { error: "You do not have permission to create admins." };
      if (e.status === 409)  return { error: e.errors[0] ?? "Email or username already taken." };
      if (e.status === 422)  return { error: e.errors.join(" ") || "Validation failed." };
      return { error: `Create failed: ${e.errors[0] ?? e.message}` };
    }
    return { error: "Unexpected error creating admin." };
  }
}
