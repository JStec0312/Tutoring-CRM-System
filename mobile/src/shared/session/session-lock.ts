export async function withSessionLock<T>(
  operation: () => Promise<T>,
): Promise<T> {
  return operation();
}
