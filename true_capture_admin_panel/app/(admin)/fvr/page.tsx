import Link from "next/link";
import { serverFetch } from "@/lib/api/server";
import type { FvrCandidateList } from "@/lib/api/types";
import { DataTable, StatusBadge, type Column } from "@/components/data-table";
import { FvrComposer } from "./composer";
import { GrantAccess } from "./grant-access";

export const dynamic = "force-dynamic";

type RawSearchParams = Record<string, string | string[] | undefined>;

export default async function FvrPage({
  searchParams,
}: {
  searchParams: Promise<RawSearchParams>;
}) {
  const sp     = await searchParams;
  const cursor = Array.isArray(sp.cursor) ? sp.cursor[0] : sp.cursor;

  const candidates = await serverFetch<FvrCandidateList>("/api/admin/fvr-candidates", {
    method: "GET",
    searchParams: { cursor },
  });

  type Row = FvrCandidateList["items"][number];
  const columns: Column<Row>[] = [
    {
      key: "user", header: "User",
      render: (r) => (
        <Link href={`/users/${r.id}`} className="hover:underline">
          <div className="font-medium">{r.displayName ?? r.username}</div>
          <div className="text-xs text-neutral-500">@{r.username}</div>
        </Link>
      ),
    },
    { key: "followers", header: "Followers", render: (r) => r.followersCount.toLocaleString() },
    { key: "score",     header: "Creator score", render: (r) => r.creatorScore.toLocaleString() },
    {
      key: "access", header: "Access",
      render: (r) => (
        <StatusBadge on={r.canPostFakeVsReal} label={r.canPostFakeVsReal ? "Granted" : "Not granted"} />
      ),
    },
    {
      key: "action", header: "",
      render: (r) => <GrantAccess userId={r.id} granted={r.canPostFakeVsReal} />,
    },
  ];

  return (
    <div className="space-y-8 max-w-5xl">
      <header>
        <h1 className="text-2xl font-semibold">Fake vs Real</h1>
        <p className="text-sm text-neutral-500">
          Publish credibility posts and manage who can upload them.
        </p>
      </header>

      <FvrComposer />

      <section className="space-y-3">
        <h2 className="font-semibold">Upload-access candidates</h2>
        <p className="text-sm text-neutral-500">
          Users flagged by the creator-score milestone — review and grant Fake vs Real upload access.
        </p>
        <DataTable
          rows={candidates.items}
          columns={columns}
          getRowKey={(r) => r.id}
          empty="No candidates yet — users appear here once their creator score crosses the threshold."
        />
        <div className="flex justify-end gap-2">
          {cursor && (
            <Link href="/fvr" className="px-3 py-1.5 rounded border text-sm">First page</Link>
          )}
          {candidates.nextCursor && (
            <Link href={`/fvr?cursor=${candidates.nextCursor}`} className="px-3 py-1.5 rounded border text-sm">
              Next →
            </Link>
          )}
        </div>
      </section>
    </div>
  );
}
