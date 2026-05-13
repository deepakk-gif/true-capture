// Tiny base64url JWT decoder — server- and edge-safe (no crypto verify, just
// reads claims). Verification is the backend's job; we only inspect the
// `role` claim to gate admin pages and surface a friendly logout message.

export type JwtClaims = {
  sub?: string;
  email?: string;
  name?: string;
  role?: string;
  exp?: number;
  [k: string]: unknown;
};

export function decodeJwtClaims(token: string): JwtClaims | null {
  const parts = token.split(".");
  if (parts.length !== 3) return null;
  try {
    const json = atob(parts[1].replace(/-/g, "+").replace(/_/g, "/"));
    return JSON.parse(json) as JwtClaims;
  } catch {
    return null;
  }
}

export function isAdminToken(token: string | undefined): boolean {
  if (!token) return false;
  const claims = decodeJwtClaims(token);
  if (!claims) return false;
  if (typeof claims.exp === "number" && claims.exp * 1000 < Date.now()) return false;
  return claims.role === "Admin";
}
