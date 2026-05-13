// Minimal data-table primitive. Re-used by every list page in the admin
// panel — Users, Posts, Approval Queue, etc. Renders a header row + body
// rows from a column definition list. Keep dependency-free.

export type Column<T> = {
  key: string;
  header: string;
  render: (row: T) => React.ReactNode;
  className?: string;
};

export function DataTable<T>({
  rows,
  columns,
  empty = "No results.",
  getRowKey,
}: {
  rows: T[];
  columns: Column<T>[];
  empty?: string;
  getRowKey: (row: T) => string | number;
}) {
  return (
    <div className="overflow-x-auto rounded-xl border border-neutral-200 dark:border-neutral-800 bg-white dark:bg-neutral-900">
      <table className="w-full text-sm">
        <thead className="bg-neutral-50 dark:bg-neutral-800/60">
          <tr>
            {columns.map((col) => (
              <th
                key={col.key}
                className={`text-left font-medium px-4 py-2 border-b border-neutral-200 dark:border-neutral-800 ${col.className ?? ""}`}
              >
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="px-4 py-8 text-center text-neutral-500">
                {empty}
              </td>
            </tr>
          ) : (
            rows.map((row) => (
              <tr key={getRowKey(row)} className="border-b border-neutral-100 dark:border-neutral-800/60 last:border-0">
                {columns.map((col) => (
                  <td key={col.key} className={`px-4 py-2 ${col.className ?? ""}`}>
                    {col.render(row)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

export function StatusBadge({ on, label }: { on: boolean; label: string }) {
  return (
    <span
      className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${
        on
          ? "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300"
          : "bg-neutral-100 text-neutral-500 dark:bg-neutral-800 dark:text-neutral-400"
      }`}
    >
      {label}
    </span>
  );
}
