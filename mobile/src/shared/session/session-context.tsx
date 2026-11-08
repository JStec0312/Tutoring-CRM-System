import { createContext, PropsWithChildren, useContext, useEffect, useSyncExternalStore } from 'react';
import { AppState } from 'react-native';
import { Session, SessionStatus } from './session-types';

const SessionContext = createContext<Session | null>(null);

export function SessionProvider({ session, children }: PropsWithChildren<{ session: Session }>) {
  useEffect(() => {
    //the function of listening to the other tabs is here
    // if triggered again, we will stop to listen to changes to avoid duplicate handling
    const unsubscribe = session.subscribeToChanges();
    void session.restoreSession();
    // listen to app state changes to refresh access token when app becomes activer
    const subscription = AppState.addEventListener('change', state => {
      if (state === 'active' && session.store.getSnapshot().status === SessionStatus.Authenticated) {
        void session.ensureAccessToken().catch(() => undefined);
      }
    });
    //when the component unmounts, we unsubscribe from session changes and remove the app state listener
    return () => {
      unsubscribe();
      subscription.remove();
    };
  }, [session]);

  return <SessionContext value={session}>{children}</SessionContext>;
}

export function useSession() {
  const session = useContext(SessionContext);
  if (!session) throw new Error('useSession must be used inside SessionProvider.');
  const snapshot = useSyncExternalStore(
    session.store.subscribe,
    session.store.getSnapshot,
    session.store.getServerSnapshot,
  );

  return {
    ...snapshot,
    isLoggedIn: snapshot.status === SessionStatus.Authenticated,
    signIn: session.signIn,
    signOut: session.signOut,
    restoreSession: session.restoreSession,
  };
}
