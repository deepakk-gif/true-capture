import Link from "next/link";
import { serverFetch, resolveMediaUrl } from "@/lib/api/server";
import type { PostReportList } from "@/lib/api/types";
import { DataTable, StatusBadge, type Column } from "@/components/data-table";
import { ReportActions } from "./report-actions";

export const dynamic = "force-dynamic";

type RawSearchParams = Record<string, string | string[] | undefined>;

function first(v: string | string[] | undefined): string | undefined {
  return Array.isArray(v) ? v[0] : v;
}

export default async function PostsModerationPage({
  searchParams,
}: {
  searchParams: Promise<RawSearchParams>;
}) {
  const sp     = await searchParams;
  const status = first(sp.status) ?? "open";
  const cursor = first(sp.cursor);

  const data = await serverFetch<PostReportList>("/api/admin/post-reports", {
    method: "GET",
    searchParams: { status, cursor },
  });

  type Row = PostReportList["items"][number];
  const columns: Column<Row>[] = [
    {
      key: "post", header: "Post",
      render: (r) => {
        const src = resolveMediaUrl(r.postCoverUrl);
        return (
          <Link href={`/users`} className="flex items-center gap-2">
            {src
              ? // eslint-disable-next-line @next/next/no-img-element
                <img src={src} alt="post" className="h-12 w-12 rounded object-cover" />
              : <div className="h-12 w-12 rounded bg-neutral-200 dark:bg-neutral-800" />}
            <span className="text-xs text-neutral-500">#{r.postId}</span>
          </Link>
        );
      },
    },
    {
      key: "reason", header: "Reason",
      render: (r) => (
        <div>
          <div className="font-medium">{r.reason}</div>
          {r.otherText && <div className="text-xs text-neutral-500">{r.otherText}</div>}
        </div>
      ),
    },
    { key: "reporter", header: "Reporter", render: (r) => <span>@{r.reporterUsername}</span> },
    {
      key: "status", header: "Status",
      render: (r) => (
        <div className="space-y-1">
          <StatusBadge on={r.status === "open"} label={r.status === "open" ? "Open" : "Resolved"} />
          {r.resolution && <div className="text-xs text-neutral-500">{r.resolution}</div>}
        </div>
      ),
    },
    {
      key: "actions", header: "Resolve",
      render: (r) => <ReportActions reportId={r.id} resolved={r.status === "resolved"} />,
    },
  ];

  return (
    <div className="space-y-6 max-w-6xl">
      <header className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Post moderation</h1>
        <div className="flex gap-2 text-sm">
          {["open", "resolved", "all"].map((s) => (
            <Link
              key={s}
              href={`/posts?status=${s}`}
              className={`px-3 py-1.5 rounded border ${
                status === s ? "bg-black text-white" : ""
              }`}
            >
              {s[0].toUpperCase() + s.slice(1)}
            </Link>
          ))}
        </div>
      </header>

      <DataTable
        rows={data.items}
        columns={columns}
        getRowKey={(r) => r.id}
        empty="No reports in this view."
      />

      <div className="flex justify-end gap-2">
        {cursor && (
          <Link href={`/posts?status=${status}`} className="px-3 py-1.5 rounded border text-sm">
            First page
          </Link>
        )}
        {data.nextCursor && (
          <Link
            href={`/posts?status=${status}&cursor=${data.nextCursor}`}
            className="px-3 py-1.5 rounded border text-sm"
          >
            Next →
          </Link>
        )}
      </div>
    </div>
  );
}
