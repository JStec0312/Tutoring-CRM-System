export function readAccessTokenRoles(accessToken: string): readonly string[] {
  const payload = accessToken.split(".")[1];
  if (!payload || !/^[A-Za-z0-9_-]+$/.test(payload)) return [];

  try {
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const bytes = atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, "="));
    const utf8 = Array.from(bytes, (byte) =>
      "%" + byte.charCodeAt(0).toString(16).padStart(2, "0"),
    ).join("");
    const role: unknown = JSON.parse(decodeURIComponent(utf8))?.role;
    const roles = Array.isArray(role) ? role : [role];

    return roles.filter((value): value is string =>
      typeof value === "string" && value.length > 0,
    );
  } catch {
    return [];
  }
}
