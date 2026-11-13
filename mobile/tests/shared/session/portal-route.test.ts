import { expect, it } from "vitest";

import { getPortalRoute } from "@/shared/session/portal-route";
import { readAccessTokenRoles } from "@/shared/session/read-access-token-roles";

it.each([
  [{ role: "Tutor" }, "/tutor"],
  [{ role: ["Administrator", "Student"] }, "/student"],
  [{ role: "Administrator" }, null],
  [{ role: "FutureRole" }, null],
  [{ role: "Parent" }, null],
  [{ role: ["Tutor", "Student"] }, null],
  [{}, null],
])("maps the JWT role claim %j to %s", (claims, expected) => {
  const payload = Buffer.from(JSON.stringify(claims)).toString("base64url");
  expect(getPortalRoute(readAccessTokenRoles("header." + payload + ".signature"))).toBe(expected);
});

it("has no portal for an unreadable access token", () => {
  expect(getPortalRoute(readAccessTokenRoles("invalid"))).toBeNull();
});
