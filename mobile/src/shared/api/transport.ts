import { ApiError, ApiMessage, ContentType, HttpHeader, ProblemDetails, RequestCredentials, RequestTimeoutMs } from './http';

type Options = { baseUrl: string; cookies: boolean; fetcher?: typeof fetch; timeoutMs?: number };
const ApiPathPrefix = '/api/';

export function createTransport({ baseUrl, cookies, fetcher = fetch, timeoutMs = RequestTimeoutMs }: Options) {
  return async function transport(path: string, init: RequestInit = {}) {
    if (!baseUrl) throw new Error(ApiMessage.Configuration);
    const origin = new URL(baseUrl);
    if (!path.startsWith(ApiPathPrefix) || path.includes('\\') || path.includes('#')) {
      throw new Error(ApiMessage.InvalidPath);
    }
    const url = new URL(path, origin);
    if (url.origin !== origin.origin || !url.pathname.startsWith(ApiPathPrefix)) throw new Error(ApiMessage.InvalidPath);

    const controller = new AbortController();
    const abort = () => controller.abort();
    if (init.signal?.aborted) controller.abort();
    init.signal?.addEventListener('abort', abort, { once: true });
    const timer = setTimeout(abort, timeoutMs);
    try {
      const response = await fetcher(url.toString(), {
        ...init, signal: controller.signal,
        credentials: cookies ? RequestCredentials.Include : RequestCredentials.Omit,
        headers: new Headers(init.headers),
      });
      if (!response.ok) {
        const problem: ProblemDetails = await response.json().catch(() => ({}));
        throw new ApiError(response.status, problem);
      }
      return response;
    } catch (error) {
      if (error instanceof ApiError || init.signal?.aborted) throw error;
      throw new Error(ApiMessage.Network);
    } finally {
      clearTimeout(timer);
      init.signal?.removeEventListener('abort', abort);
    }
  };
}

export type Transport = ReturnType<typeof createTransport>;
export const jsonHeaders = { [HttpHeader.ContentType]: ContentType.Json };
