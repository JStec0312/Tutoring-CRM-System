import { SessionEvent, SessionEventValue, SessionSyncKey } from './session-sync-types';
export { SessionEvent } from './session-sync-types';

export const sessionSync = {
  publish(event: SessionEventValue) {
    try { localStorage.setItem(SessionSyncKey, JSON.stringify({ event, nonce: Math.random() })); }
    catch { /* Credential/marker storage errors are handled by the auth workflow. */ }
  },
  subscribe(listener: (event: SessionEventValue) => void) {
    const onStorage = (event: StorageEvent) => {
      if (event.key !== SessionSyncKey || !event.newValue) return;
      try {
        const message = JSON.parse(event.newValue);
        if (Object.values(SessionEvent).includes(message.event)) listener(message.event);
      } catch { /* Ignore invalid messages; storage never carries credentials. */ }
    };
    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  },
};
