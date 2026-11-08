import { Session, SessionError } from '../session/session-types';
import { HttpHeader, HttpMethod, HttpMethodValue, HttpStatus, isUnauthorized } from './http';
import { jsonHeaders, Transport } from './transport';

type Options = {
  transport: Transport;
  session: Pick<Session, 'store' | 'ensureAccessToken' | 'refresh' | 'invalidate'>;
};
type RequestOptions = { signal?: AbortSignal };

export function createApiClient({ transport, session }: Options) {
  const { store } = session;
  async function request<T>(method: HttpMethodValue, path: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
    const generation = store.getGeneration();
    let accessToken = await session.ensureAccessToken();
    store.assertCurrent(generation);
    const payload = body === undefined ? undefined : JSON.stringify(body);

    async function send() {
      store.assertCurrent(generation);
      const response = await transport(path, {
        method, signal: options.signal, body: payload,
        headers: { ...jsonHeaders, [HttpHeader.Authorization]: `Bearer ${accessToken}` },
      });
      const result = response.status === HttpStatus.NoContent ? undefined : await response.json();
      store.assertCurrent(generation);
      return result as T;
    }

    try {
      return await send();
    } catch (error) {
      store.assertCurrent(generation);
      if (!isUnauthorized(error) || options.signal?.aborted) throw error;
      if (store.getToken()?.value === accessToken) await session.refresh();
      store.assertCurrent(generation);
      accessToken = store.getToken()?.value ?? '';
      if (!accessToken) throw new Error(SessionError.Required);
      try {
        return await send(); // only replay explicit unauthorized responses; never network failures
      } catch (retryError) {
        if (isUnauthorized(retryError) && generation === store.getGeneration()) await session.invalidate();
        throw retryError;
      }
    }
  }

  return {
    get: <T>(path: string, options?: RequestOptions) => request<T>(HttpMethod.Get, path, undefined, options),
    post: <T = void>(path: string, body?: unknown, options?: RequestOptions) => request<T>(HttpMethod.Post, path, body, options),
    put: <T = void>(path: string, body?: unknown, options?: RequestOptions) => request<T>(HttpMethod.Put, path, body, options),
    delete: <T = void>(path: string, options?: RequestOptions) => request<T>(HttpMethod.Delete, path, undefined, options),
  };
}

export type ApiClient = ReturnType<typeof createApiClient>;
