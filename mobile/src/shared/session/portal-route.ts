export function getPortalRoute(roles: readonly string[]) {
  const isTutor = roles.includes("Tutor");
  const isStudent = roles.includes("Student");
  const isParent = roles.includes("Parent");
  if (isTutor === isStudent && isStudent === isParent) return null;
  if (isTutor) return "/tutor";
  if (isStudent) return "/student";
  if (isParent) return "/parent";
}
