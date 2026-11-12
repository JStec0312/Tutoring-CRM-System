import {
  createContext,
  useContext,
  useEffect,
  useSyncExternalStore,
  type PropsWithChildren,
} from "react";

import type { SessionManager } from "./session-manager";

const AuthContext = createContext<SessionManager | null>(null);

type Props = PropsWithChildren<{
  manager: SessionManager;
}>;

export function AuthProvider({ manager, children }: Props) {
  useEffect(() => {
    void manager.restoreSession();
  }, [manager]);

  return (
    <AuthContext.Provider value={manager}>{children}</AuthContext.Provider>
  );
}

export function useSessionManager(): SessionManager {
  const manager = useContext(AuthContext);

  if (!manager) {
    throw new Error("AuthProvider is missing");
  }

  return manager;
}

export function useAuth() {
  const manager = useSessionManager();

  const snapshot = useSyncExternalStore(
    manager.subscribe,
    manager.getSnapshot,
    manager.getSnapshot,
  );

  return {
    ...snapshot,
    isAuthenticated: snapshot.status === "authenticated",
    signIn: manager.signIn,
    signOut: manager.signOut,
  };
}
