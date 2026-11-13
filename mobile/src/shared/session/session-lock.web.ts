const LOCK_NAME = "tutoring.session.credentials";

export async function withSessionLock<T>(
  operation: () => Promise<T>,
): Promise<T> {
  if (typeof navigator === "undefined" || !navigator.locks) {
    throw new Error("Session.WebLockUnavailable");
  }

  return navigator.locks.request(LOCK_NAME, { mode: "exclusive" }, operation);
}
