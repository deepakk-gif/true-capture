import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { serverFetch, ACCESS_COOKIE } from "@/lib/api/server";
import { hasPermission } from "@/lib/jwt";
import { NewAdminForm } from "./new-admin-form";

export const dynamic = "force-dynamic";

type PermissionDescriptor = {
  code: string;
  module: string;
  description: string | null;
};

export default async function NewAdminPage() {
  // Page-level gate. The middleware already blocks non-admins; this extra
  // check prevents a plain admin (no Users.CreateAdmin grant) from rendering
  // a useless form whose submission would 403 anyway.
  const jar    = await cookies();
  const access = jar.get(ACCESS_COOKIE)?.value;
  if (!hasPermission(access, "Users.CreateAdmin")) {
    redirect("/users");
  }

  const permissions = await serverFetch<PermissionDescriptor[]>(
    "/api/admin/permissions",
    { method: "GET" },
  );

  // Group by module so the picker reads cleanly.
  const groupedMap = new Map<string, PermissionDescriptor[]>();
  for (const p of permissions) {
    const list = groupedMap.get(p.module) ?? [];
    list.push(p);
    groupedMap.set(p.module, list);
  }
  const permissionGroups = Array.from(groupedMap.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([module, perms]) => ({ module, perms }));

  return (
    <div className="space-y-6 max-w-3xl">
      <header>
        <h1 className="text-2xl font-semibold">New admin</h1>
        <p className="text-sm text-neutral-500 mt-1">
          Only super-admins can mint new admins. The admin you create will sign
          in with the email + initial password you set, and inherits exactly
          the permissions you check below — nothing more.
        </p>
      </header>

      <NewAdminForm permissionGroups={permissionGroups} />
    </div>
  );
}
