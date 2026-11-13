// This file contains the implementation of the API context for React.
// It provides the ApiProvider component and the useApi hook.
// The ApiProvider component should wrap the part of the app that needs access to the API client.
// The useApi hook allows consuming components to access the API client.
// Every component that needs to use the API client must be wrapped by the ApiProvider.
// than all it needs to do is const api = useApi();
import { createContext, useContext, type PropsWithChildren } from "react";

import type { ApiClient } from "./api-client";

const ApiContext = createContext<ApiClient | null>(null);

type Props = PropsWithChildren<{
  client: ApiClient;
}>;

export function ApiProvider({ client, children }: Props) {
  return <ApiContext.Provider value={client}>{children}</ApiContext.Provider>;
}

export function useApi(): ApiClient {
  const api = useContext(ApiContext);

  if (!api) {
    throw new Error("ApiProvider is missing");
  }

  return api;
}
