import Link from "next/link";
import { cookies } from "next/headers";
import { serverFetch, resolveMediaUrl, ACCESS_COOKIE } from "@/lib/api/server";
import { hasPermission } from "@/lib/jwt";
import type { AdminUserDetail, AdminPostList } from "@/lib/api/types";
import { UserDetailView } from "./user-detail-form";

export const dynamic = "force-dynamic";

export default async function UserDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;

  // Admin report bypasses privacy — the backend returns full data + posts.
  const [user, postList] = await Promise.all([
    serverFetch<AdminUserDetail>(`/api/admin/users/${id}`, { method: "GET" }),
    serverFetch<AdminPostList>(`/api/admin/users/${id}/posts`, { method: "GET" }),
  ]);

  // Resolve media URLs server side — the client component must not import
  // lib/api/server.ts (it pulls in next/headers).
  const avatarSrc = resolveMediaUrl(user.avatarUrl);
  const posts = postList.items.map((p) => ({
    id: p.id,
    imageSrc: resolveMediaUrl(p.coverUrl),
    caption: p.caption,
  }));

  const jar       = await cookies();
  const canManage = hasPermission(jar.get(ACCESS_COOKIE)?.value, "Users.Manage");

  return (
    <div className="space-y-6 max-w-3xl">
      <header className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">User detail</h1>
        <Link href="/users" className="text-sm text-neutral-500 hover:underline">
          ← Back to users
        </Link>
      </header>

      {!canManage && (
        <div className="text-sm text-amber-700 bg-amber-50 dark:bg-amber-950/40 p-3 rounded">
          You have read-only access. Editing requires the{" "}
          <code>Users.Manage</code> permission.
        </div>
      )}

      <UserDetailView
        user={user}
        avatarSrc={avatarSrc}
        posts={posts}
        canManage={canManage}
      />
    </div>
  );
}
