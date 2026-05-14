import Link from "next/link";
import { cookies } from "next/headers";
import { logoutAction } from "@/app/login/actions";
import { ACCESS_COOKIE } from "@/lib/api/server";
import { hasPermission } from "@/lib/jwt";

type NavItem = { href: string; label: string; requires?: string };

const NAV: NavItem[] = [
  { href: "/users",         label: "Users" },
  { href: "/users/new",     label: "New admin", requires: "Users.CreateAdmin" },
  { href: "/posts",         label: "Posts" },
  { href: "/approval",      label: "Approval queue" },
  { href: "/fvr",           label: "Fake vs Real" },
  { href: "/analytics",     label: "Analytics" },
  { href: "/announcements", label: "Announcements" },
  { href: "/email",         label: "Email" },
  { href: "/taxonomy",      label: "Taxonomy" },
  { href: "/cms",           label: "CMS" },
  { href: "/contact",       label: "Contact" },
  { href: "/audit",         label: "Audit log" },
];

export async function Sidebar() {
  const jar    = await cookies();
  const access = jar.get(ACCESS_COOKIE)?.value;

  const visible = NAV.filter((it) => !it.requires || hasPermission(access, it.requires));

  return (
    <aside className="w-60 shrink-0 border-r border-neutral-200 dark:border-neutral-800 bg-white dark:bg-neutral-900 min-h-screen flex flex-col">
      <div className="px-4 py-4 font-semibold text-lg border-b border-neutral-200 dark:border-neutral-800">
        True Capture
      </div>
      <nav className="flex-1 px-2 py-3 space-y-1">
        {visible.map((it) => (
          <Link
            key={it.href}
            href={it.href as never}
            className="block px-3 py-2 rounded text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800"
          >
            {it.label}
          </Link>
        ))}
      </nav>
      <form action={logoutAction} className="border-t border-neutral-200 dark:border-neutral-800 p-3">
        <button
          type="submit"
          className="w-full text-left px-3 py-2 rounded text-sm hover:bg-neutral-100 dark:hover:bg-neutral-800"
        >
          Log out
        </button>
      </form>
    </aside>
  );
}
