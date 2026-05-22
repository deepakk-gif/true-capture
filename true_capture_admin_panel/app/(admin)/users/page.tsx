import Link from "next/link";
import { serverFetch } from "@/lib/api/server";
import type { AdminUserListResult, AdminUserListQuery } from "@/lib/api/types";
import { DataTable, StatusBadge, type Column } from "@/components/data-table";

export const dynamic = "force-dynamic";

type RawSearchParams = Record<string, string | string[] | undefined>;

function first(v: string | string[] | undefined): string | undefined {
  if (Array.isArray(v)) return v[0];
  return v;
}

function tribool(v: string | undefined): boolean | undefined {
  if (v === "true")  return true;
  if (v === "false") return false;
  return undefined;
}

function buildQuery(sp: RawSearchParams): AdminUserListQuery {
  return {
    search:     first(sp.search) ?? "",
    isActive:   tribool(first(sp.isActive)),
    isAdmin:    tribool(first(sp.isAdmin)),
    isVerified: tribool(first(sp.isVerified)),
    hasGoogle:  tribool(first(sp.hasGoogle)),
    cursor:     first(sp.cursor),
    limit:      20,
  };
}

function paramsToSearch(p: Record<string, string | undefined>): string {
  const usp = new URLSearchParams();
  for (const [k, v] of Object.entries(p)) {
    if (v !== undefined && v !== "") usp.set(k, v);
  }
  const s = usp.toString();
  return s ? `?${s}` : "";
}

export default async function UsersPage({
  searchParams,
}: {
  searchParams: Promise<RawSearchParams>;
}) {
  const sp = await searchParams;
  const q  = buildQuery(sp);

  const data = await serverFetch<AdminUserListResult>("/api/admin/users", {
    method: "GET",
    searchParams: {
      search:     q.search,
      isActive:   q.isActive,
      isAdmin:    q.isAdmin,
      isVerified: q.isVerified,
      hasGoogle:  q.hasGoogle,
      cursor:     q.cursor,
      limit:      q.limit,
    },
  });

  const columns: Column<AdminUserListResult["items"][number]>[] = [
    { key: "user",     header: "User",      render: (r) => (
        <Link href={`/users/${r.id}`} className="block hover:underline">
          <div className="font-medium">{r.displayName ?? r.username}</div>
          <div className="text-xs text-neutral-500">@{r.username} · {r.email}</div>
        </Link>
      ),
    },
    { key: "status",   header: "Status",    render: (r) => (
        <div className="flex flex-wrap gap-1">
          <StatusBadge on={r.isActive}      label={r.isActive ? "Active" : "Banned"} />
          {r.isAdmin    && <StatusBadge on label="Admin" />}
          {r.isVerified && <StatusBadge on label="Verified" />}
          {r.hasGoogle  && <StatusBadge on label="Google" />}
          {!r.emailVerified && <StatusBadge on={false} label="Email unverified" />}
        </div>
      ),
    },
    { key: "lastLogin", header: "Last login", render: (r) =>
        r.lastLoginAtUtc ? new Date(r.lastLoginAtUtc).toLocaleString() : "—",
    },
    { key: "created",  header: "Joined", render: (r) =>
        new Date(r.createdAtUtc).toLocaleDateString(),
    },
  ];

  // Build "preserve filters, swap cursor" links for pagination.
  const baseParams = {
    search:     q.search,
    isActive:   q.isActive === undefined ? undefined : String(q.isActive),
    isAdmin:    q.isAdmin    === undefined ? undefined : String(q.isAdmin),
    isVerified: q.isVerified === undefined ? undefined : String(q.isVerified),
    hasGoogle:  q.hasGoogle  === undefined ? undefined : String(q.hasGoogle),
  };

  return (
    <div className="space-y-6 max-w-6xl">
      <header className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Users</h1>
        <div className="text-sm text-neutral-500">{data.total.toLocaleString()} total</div>
      </header>

      <FilterBar current={q} />

      <DataTable
        rows={data.items}
        columns={columns}
        getRowKey={(r) => r.id}
        empty="No users match the current filters."
      />

      <div className="flex items-center justify-end gap-2">
        {q.cursor && (
          <Link
            href={`/users${paramsToSearch(baseParams)}`}
            className="px-3 py-1.5 rounded border text-sm"
          >
            First page
          </Link>
        )}
        {data.nextCursor && (
          <Link
            href={`/users${paramsToSearch({ ...baseParams, cursor: data.nextCursor })}`}
            className="px-3 py-1.5 rounded border text-sm"
          >
            Next →
          </Link>
        )}
      </div>
    </div>
  );
}

function FilterBar({ current }: { current: AdminUserListQuery }) {
  // Plain server-rendered <form method="GET"> — submit reloads the page
  // with the chosen query string, which the page reads back via searchParams.
  return (
    <form method="GET" className="grid grid-cols-2 md:grid-cols-6 gap-3 items-end">
      <label className="flex flex-col text-sm md:col-span-2">
        <span className="text-neutral-500 mb-1">Search</span>
        <input
          name="search"
          defaultValue={current.search ?? ""}
          placeholder="email or username"
          className="rounded border px-2 py-1.5 bg-transparent"
        />
      </label>
      <TriSelect name="isActive"   label="Active"     value={current.isActive} />
      <TriSelect name="isAdmin"    label="Admin"      value={current.isAdmin} />
      <TriSelect name="isVerified" label="Verified"   value={current.isVerified} />
      <TriSelect name="hasGoogle"  label="Has Google" value={current.hasGoogle} />
      <div className="flex gap-2">
        <button type="submit" className="px-3 py-1.5 rounded bg-black text-white text-sm">
          Apply
        </button>
        <Link href="/users" className="px-3 py-1.5 rounded border text-sm">
          Reset
        </Link>
      </div>
    </form>
  );
}

function TriSelect({
  name, label, value,
}: { name: string; label: string; value: boolean | undefined }) {
  return (
    <label className="flex flex-col text-sm">
      <span className="text-neutral-500 mb-1">{label}</span>
      <select
        name={name}
        defaultValue={value === undefined ? "" : String(value)}
        className="rounded border px-2 py-1.5 bg-transparent"
      >
        <option value="">Any</option>
        <option value="true">Yes</option>
        <option value="false">No</option>
      </select>
    </label>
  );
}
