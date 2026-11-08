import { createContext, PropsWithChildren, useContext } from 'react';
import { ApiClient } from './api-client';

const ApiContext = createContext<ApiClient | null>(null);

export function ApiProvider({ client, children }: PropsWithChildren<{ client: ApiClient }>) {
  return <ApiContext value={client}>{children}</ApiContext>;
}

export function useApi() {
  const client = useContext(ApiContext);
  if (!client) throw new Error('useApi must be used inside ApiProvider.');
  return client;
}
