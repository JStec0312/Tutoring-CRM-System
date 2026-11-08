import { createAuthApi } from '@/features/auth/api/auth-api';
import { createAuthController } from '@/features/auth/models/auth-controller';
import { createApiClient } from '@/shared/api/api-client';
import { createTransport } from '@/shared/api/transport';
import { credentialStorage } from '@/shared/session/credential-storage';
import { sessionSync } from '@/shared/session/session-sync';
import { resolveApiOrigin } from './api-origin';

const browserOrigin = !credentialStorage.native && typeof window !== 'undefined' ? window.location.origin : undefined;
const baseUrl = resolveApiOrigin(process.env.EXPO_PUBLIC_API_URL?.trim() ?? '', browserOrigin);
const transport = createTransport({ baseUrl, cookies: !credentialStorage.native });
const authApi = createAuthApi(transport, credentialStorage.native);
export const session = createAuthController({
  api: authApi,
  storage: credentialStorage,
  sync: sessionSync,
  configured: Boolean(baseUrl),
});
export const api = createApiClient({ transport, session });
