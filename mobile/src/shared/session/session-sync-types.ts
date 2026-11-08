export const SessionEvent = { Changed: 'changed', SignedOut: 'signed-out' } as const;
export type SessionEventValue = (typeof SessionEvent)[keyof typeof SessionEvent];
export const SessionSyncKey = 'tutoring.session.event';

export type SessionSync = {
  publish(event: SessionEventValue): void;
  subscribe(listener: (event: SessionEventValue) => void): () => void;
};
