import { SessionEventValue } from './session-sync-types';
export { SessionEvent } from './session-sync-types';

// Native has no browser tabs; keep the same interface without cross-tab work.
export const sessionSync = {
  publish(_event: SessionEventValue) {},
  subscribe(_listener: (event: SessionEventValue) => void) { return () => {}; },
};
