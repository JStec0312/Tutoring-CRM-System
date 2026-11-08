import { HttpMethod } from '../../../shared/api/http';
import { jsonHeaders, Transport } from '../../../shared/api/transport';
import { Credentials } from '../../../shared/session/session-types';

export const AuthEndpoint = {
  web: { login: '/api/auth/login', refresh: '/api/auth/refresh', signOut: '/api/auth/sign-out' },
  native: { login: '/api/auth/mobile/login', refresh: '/api/auth/mobile/refresh', signOut: '/api/auth/mobile/sign-out' },
} as const;

export interface AuthApi {
  login(credentials: Credentials): Promise<unknown>;
  refresh(refreshToken: string | null): Promise<unknown>;
  signOut(refreshToken: string | null): Promise<void>;
}

export function createAuthApi(transport: Transport, native: boolean): AuthApi {
  const endpoints = native ? AuthEndpoint.native : AuthEndpoint.web;
  async function post(path: string, body?: unknown) {
    return transport(path, {
      method: HttpMethod.Post, headers: jsonHeaders,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  }
  const refreshBody = (refreshToken: string | null) => native ? { refreshToken } : undefined;
  return {
    login: async credentials => (await post(endpoints.login, credentials)).json(),
    refresh: async refreshToken => (await post(endpoints.refresh, refreshBody(refreshToken))).json(),
    signOut: async refreshToken => { await post(endpoints.signOut, refreshBody(refreshToken)); },
  };
}
