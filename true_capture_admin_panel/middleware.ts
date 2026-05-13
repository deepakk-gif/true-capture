import { NextRequest, NextResponse } from "next/server";
import { isAdminToken } from "@/lib/jwt";

const ACCESS_COOKIE = "tc_access";

const PUBLIC_PATHS = new Set(["/login", "/api/login"]);

export function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;

  // Allow static assets / API routes that are explicitly public.
  if (
    pathname.startsWith("/_next/") ||
    pathname.startsWith("/favicon") ||
    PUBLIC_PATHS.has(pathname)
  ) {
    return NextResponse.next();
  }

  const access = req.cookies.get(ACCESS_COOKIE)?.value;
  if (!access) {
    const login = req.nextUrl.clone();
    login.pathname = "/login";
    login.searchParams.set("from", pathname);
    return NextResponse.redirect(login);
  }

  if (!isAdminToken(access)) {
    // Signed in, but not an admin — clear cookies and surface the reason.
    const login = req.nextUrl.clone();
    login.pathname = "/login";
    login.searchParams.set("reason", "not_admin");
    const res = NextResponse.redirect(login);
    res.cookies.delete(ACCESS_COOKIE);
    res.cookies.delete("tc_refresh");
    return res;
  }

  return NextResponse.next();
}

export const config = {
  // Run on everything except Next internals.
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
