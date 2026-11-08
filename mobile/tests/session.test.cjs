const { test } = require('node:test');
const assert = require('node:assert/strict');
const { resolveApiOrigin } = require('../.test-build/bootstrap/api-origin');
const { SessionStatus, SessionChangedError } = require('../.test-build/shared/session/session-types');
const { ApiError } = require('../.test-build/shared/api/http');
const { createApiClient } = require('../.test-build/shared/api/api-client');
const { createTransport } = require('../.test-build/shared/api/transport');
const { createAuthApi } = require('../.test-build/features/auth/api/auth-api');
const { createAuthController } = require('../.test-build/features/auth/models/auth-controller');
const { mapSession } = require('../.test-build/features/auth/models/session-mapper');

const now = Date.parse('2026-11-05T12:00:00Z');

test('local web previews keep their API host aligned for SameSite refresh cookies', () => {
  assert.equal(resolveApiOrigin('http://localhost:5106', 'http://127.0.0.1:8081'), 'http://127.0.0.1:5106');
  assert.equal(resolveApiOrigin('http://127.0.0.1:5106', 'http://localhost:8081'), 'http://localhost:5106');
  assert.equal(resolveApiOrigin('http://localhost:5106'), 'http://localhost:5106');
  assert.equal(resolveApiOrigin('https://api.example.test', 'http://127.0.0.1:8081'), 'https://api.example.test');
  assert.equal(resolveApiOrigin('http://localhost:5106', 'https://app.example.test'), 'http://localhost:5106');
  assert.equal(resolveApiOrigin('', 'http://127.0.0.1:8081'), '');
});

const credentials = { email: 'student@example.test', password: 'example-password' };
const deferred = () => {
  let resolve, reject;
  const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
};
function response(name = 'initial', expiry = now + 600_000) {
  const claims = { sub: name, email: credentials.email, role: ['Student', 'Tutor'] };
  return {
    accessToken: `header.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.${name}`,
    expiresAt: new Date(expiry).toISOString(), refreshToken: `refresh-${name}`,
  };
}
function fixture({ existing = null, native = true, configured = true } = {}) {
  const state = { credential: existing, pending: false, refreshes: 0, revoked: [], failWrite: false };
  const storage = {
    native,
    readRefreshToken: async () => state.credential,
    writeRefreshToken: async value => { if (state.failWrite) throw new Error('Keychain failure'); state.credential = value; },
    deleteRefreshToken: async () => { state.credential = null; },
    isLogoutPending: async () => state.pending,
    setLogoutPending: async value => { state.pending = value; },
    exclusive: operation => operation(),
  };
  const authApi = {
    login: async () => response(),
    refresh: async () => { state.refreshes++; return response('rotated'); },
    signOut: async value => { state.revoked.push(value); },
  };
  const events = [];
  const listeners = new Set();
  const sync = {
    publish: event => events.push(event),
    subscribe(listener) { listeners.add(listener); return () => listeners.delete(listener); },
  };
  const receive = event => listeners.forEach(listener => listener(event));
  const auth = createAuthController({ api: authApi, storage, sync, configured, now: () => now });
  const store = auth.store;
  const client = transport => createApiClient({ transport, session: auth });
  return { state, storage, authApi, store, auth, client, sync, events, receive, listeners };
}

test('native restoration with no credential shows sign-in without a network call', async () => {
  const f = fixture(); await f.auth.restoreSession();
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.equal(f.state.refreshes, 0);
});
test('restoration rotates stored refresh token and exposes identity, not tokens, to UI', async () => {
  const f = fixture({ existing: 'old' });
  await Promise.all([f.auth.restoreSession(), f.auth.restoreSession()]);
  assert.equal(f.state.refreshes, 1);
  assert.equal(f.state.credential, 'refresh-rotated');
  assert.equal(f.store.getSnapshot().user.id, 'rotated');
  assert.equal('accessToken' in f.store.getSnapshot(), false);
});
test('offline restoration preserves credentials and allows a successful retry', async () => {
  const f = fixture({ existing: 'old' });
  f.authApi.refresh = async () => { throw new Error('Offline'); };
  await f.auth.restoreSession();
  assert.equal(f.store.getSnapshot().status, SessionStatus.RestoreError);
  assert.equal(f.state.credential, 'old');
  f.authApi.refresh = async () => response('retry');
  await f.auth.restoreSession();
  assert.equal(f.store.getSnapshot().status, SessionStatus.Authenticated);
});
test('rejected refresh clears credentials and notifies private-data cleanup', async () => {
  const f = fixture({ existing: 'old' }); let cleared = 0;
  f.store.onClear(() => cleared++);
  f.authApi.refresh = async () => { throw new ApiError(401); };
  await f.auth.restoreSession();
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.equal(f.state.credential, null);
  assert.equal(cleared, 1);
});
test('concurrent unauthorized requests share one refresh and replay with the latest token', async () => {
  const f = fixture(); await f.auth.signIn(credentials);
  const gate = deferred();
  f.authApi.refresh = async () => { f.state.refreshes++; await gate.promise; return response('rotated'); };
  const seen = [];
  const client = f.client(async (_path, init) => {
    seen.push(init.headers.Authorization);
    if (init.headers.Authorization.includes('.initial')) throw new ApiError(401);
    return new Response(JSON.stringify({ ok: true }));
  });
  const requests = [client.get('/api/test'), client.get('/api/test')];
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(f.state.refreshes, 1);
  gate.resolve();
  assert.deepEqual(await Promise.all(requests), [{ ok: true }, { ok: true }]);
  assert.equal(seen.length, 4);
  assert.ok(seen.slice(2).every(value => value.endsWith('.rotated')));
});
test('expired access token refreshes before the protected request', async () => {
  const f = fixture(); f.authApi.login = async () => response('initial', now + 1_000);
  await f.auth.signIn(credentials);
  let sent;
  const client = f.client(async (_path, init) => { sent = init.headers.Authorization; return new Response('{}'); });
  await client.get('/api/test');
  assert.equal(f.state.refreshes, 1);
  assert.ok(sent.endsWith('.rotated'));
});
test('403 and network failures do not refresh or replay writes', async () => {
  for (const error of [new ApiError(403), new Error('Offline')]) {
    const f = fixture(); await f.auth.signIn(credentials); let calls = 0;
    const client = f.client(async () => { calls++; throw error; });
    await assert.rejects(client.post('/api/test', { value: 1 }));
    assert.equal(calls, 1); assert.equal(f.state.refreshes, 0);
    assert.equal(f.store.getSnapshot().status, SessionStatus.Authenticated);
  }
});
test('a second 401 ends the session, with no refresh loop', async () => {
  const f = fixture(); await f.auth.signIn(credentials); let requests = 0;
  const client = f.client(async () => { requests++; throw new ApiError(401); });
  await assert.rejects(client.get('/api/test'));
  assert.equal(requests, 2); assert.equal(f.state.refreshes, 1);
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
});
test('logout during refresh never restores session; revokes the rotated credential', async () => {
  const f = fixture(); await f.auth.signIn(credentials);
  const gate = deferred();
  f.authApi.refresh = async () => { await gate.promise; return response('late'); };
  const refreshing = f.auth.refresh();
  const rejection = assert.rejects(refreshing, SessionChangedError);
  await new Promise(resolve => setImmediate(resolve));
  const signingOut = f.auth.signOut();
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  gate.resolve(); await rejection; await signingOut;
  assert.equal(f.state.credential, null);
  assert.deepEqual(f.state.revoked, ['refresh-late']);
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
});
test('late protected response after logout is discarded', async () => {
  const f = fixture(); await f.auth.signIn(credentials); const gate = deferred();
  const client = f.client(async () => { await gate.promise; return new Response('{"private":true}'); });
  const request = client.get('/api/test');
  const rejection = assert.rejects(request, SessionChangedError);
  await new Promise(resolve => setImmediate(resolve));
  await f.auth.signOut(); gate.resolve(); await rejection;
});
test('storage write failure fails closed and attempts to revoke the issued token', async () => {
  const f = fixture(); f.state.failWrite = true;
  await assert.rejects(f.auth.signIn(credentials));
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.equal(f.state.credential, null);
  assert.deepEqual(f.state.revoked, ['refresh-initial']);
});
test('failed server logout clears local credentials and blocks restoration after restart', async () => {
  const f = fixture(); await f.auth.signIn(credentials);
  f.authApi.signOut = async () => { throw new Error('Offline'); };
  await assert.rejects(f.auth.signOut());
  assert.equal(f.state.pending, true); assert.equal(f.state.credential, null);
  const restarted = createAuthController({ api: f.authApi, storage: f.storage, sync: f.sync, now: () => now });
  await restarted.restoreSession();
  assert.equal(restarted.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.equal(f.state.refreshes, 0);
});
test('explicit login recovers after a pending logout', async () => {
  const f = fixture(); f.state.pending = true;
  await f.auth.restoreSession(); await f.auth.signIn(credentials);
  assert.equal(f.state.pending, false);
  assert.ok(await f.auth.ensureAccessToken());
});
test('mapper rejects malformed sessions and interprets backend timestamps as UTC', () => {
  assert.throws(() => mapSession({ accessToken: 'bad', expiresAt: 'bad' }, true, now));
  assert.throws(() => mapSession({ ...response(), refreshToken: null }, true, now));
  const dto = response(); dto.expiresAt = '2026-11-05T12:10:00';
  assert.equal(mapSession(dto, true, now).token.expiresAt, now + 600_000);
});
test('mobile auth uses JSON credentials, web auth keeps refresh tokens in cookies', async () => {
  for (const native of [true, false]) {
    const calls = [];
    const api = createAuthApi(async (path, init) => { calls.push([path, init]); return new Response('{}'); }, native);
    await api.refresh('secret');
    assert.equal(calls[0][0], native ? '/api/auth/mobile/refresh' : '/api/auth/refresh');
    assert.equal(calls[0][1].body, native ? '{"refreshToken":"secret"}' : undefined);
  }
});
test('transport rejects external URLs before sending an access token', async () => {
  let calls = 0;
  const transport = createTransport({ baseUrl: 'https://api.example.test', cookies: false,
    fetcher: async () => { calls++; return new Response('{}'); } });
  for (const path of ['https://evil.test/api/test', '//evil.test/api/test', '/other', '/api/../private', '/api/%2e%2e/private']) {
    await assert.rejects(transport(path));
  }
  assert.equal(calls, 0);
});

test('pending-logout marker failure does not prevent native credential deletion', async () => {
  const f = fixture(); await f.auth.signIn(credentials);
  f.storage.setLogoutPending = async () => { throw new Error('Storage failure'); };
  await assert.rejects(f.auth.signOut());
  assert.equal(f.state.credential, null);
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.deepEqual(f.state.revoked, ['refresh-initial']);
});
test('malformed refresh response fails closed instead of leaving an expired authenticated session', async () => {
  const f = fixture(); await f.auth.signIn(credentials);
  f.authApi.refresh = async () => ({ accessToken: 'malformed', expiresAt: 'bad' });
  await assert.rejects(f.auth.refresh());
  assert.equal(f.state.credential, null);
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
});
test('transient refresh failure preserves current credentials for a later retry', async () => {
  const f = fixture(); await f.auth.signIn(credentials);
  f.authApi.refresh = async () => { throw new ApiError(503); };
  await assert.rejects(f.auth.refresh());
  assert.equal(f.state.credential, 'refresh-initial');
  assert.equal(f.store.getSnapshot().status, SessionStatus.Authenticated);
});
test('next account login waits for obsolete refresh and keeps the new account credential', async () => {
  const f = fixture(); await f.auth.signIn(credentials); const gate = deferred();
  f.authApi.refresh = async () => { await gate.promise; return response('old-late'); };
  const rejection = assert.rejects(f.auth.refresh(), SessionChangedError);
  await new Promise(resolve => setImmediate(resolve));
  f.authApi.login = async () => response('new-account');
  const login = f.auth.signIn(credentials);
  gate.resolve(); await rejection; await login;
  assert.equal(f.state.credential, 'refresh-new-account');
  assert.equal(f.store.getSnapshot().user.id, 'new-account');
});

test('JWT identity decoding works without browser globals and preserves Unicode', () => {
  const previous = globalThis.atob;
  try {
    globalThis.atob = undefined;
    const claims = { sub: 'unicode-user', email: 'uczeń@example.test', role: 'Student' };
    const dto = response();
    dto.accessToken = `h.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.s`;
    assert.equal(mapSession(dto, true, now).user.email, claims.email);
  } finally { globalThis.atob = previous; }
});

test('successful login announces the committed session; failed login announces nothing', async () => {
  const f = fixture();
  f.sync.publish = event => {
    assert.equal(f.store.getSnapshot().status, SessionStatus.Authenticated);
    f.events.push(event);
  };
  f.authApi.login = async () => { throw new ApiError(401); };
  await assert.rejects(f.auth.signIn(credentials));
  assert.deepEqual(f.events, []);
  f.authApi.login = async () => response();
  await f.auth.signIn(credentials);
  assert.deepEqual(f.events, ['changed']);
});

test('logout announces and clears immediately, even while server cleanup is pending', async () => {
  const f = fixture();
  await f.auth.signIn(credentials);
  const gate = deferred();
  f.authApi.signOut = () => gate.promise;
  const logout = f.auth.signOut();
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.deepEqual(f.events, ['changed', 'signed-out']);
  await f.auth.restoreSession();
  await assert.rejects(f.auth.ensureAccessToken());
  await assert.rejects(f.auth.refresh());
  assert.equal(f.state.refreshes, 0);
  gate.resolve();
  await logout;
});

test('restoration is idempotent after authenticated and anonymous outcomes', async () => {
  for (const existing of [null, 'saved']) {
    const f = fixture({ existing });
    await f.auth.restoreSession();
    const snapshot = f.store.getSnapshot();
    await f.auth.restoreSession();
    assert.equal(f.state.refreshes, existing ? 1 : 0);
    assert.equal(f.store.getSnapshot(), snapshot);
  }
});

test('missing API configuration leaves session anonymous without touching storage or API', async () => {
  const f = fixture({ existing: 'saved', configured: false });
  f.storage.isLogoutPending = async () => { throw new Error('Should not read storage'); };
  await f.auth.restoreSession();
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.equal(f.state.refreshes, 0);
});

test('web restoration uses the cookie even though JavaScript reads no refresh token', async () => {
  const f = fixture({ native: false });
  f.storage.writeRefreshToken = async () => { throw new Error('Never store web tokens'); };
  await f.auth.restoreSession();
  assert.equal(f.state.refreshes, 1);
  assert.equal(f.store.getSnapshot().status, SessionStatus.Authenticated);
  assert.equal(f.state.credential, null);
});

test('a pending web logout prevents cookie restoration', async () => {
  const f = fixture({ native: false });
  f.state.pending = true;
  await f.auth.restoreSession();
  assert.equal(f.state.refreshes, 0);
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
});

test('other-tab logout clears this tab without rebroadcasting or contacting the server', async () => {
  const f = fixture({ native: false });
  await f.auth.signIn(credentials);
  const unsubscribe = f.auth.subscribeToChanges();
  f.receive('signed-out');
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  await f.auth.restoreSession();
  await assert.rejects(f.auth.refresh());
  assert.equal(f.state.refreshes, 0);
  assert.deepEqual(f.events, ['changed']);
  assert.deepEqual(f.state.revoked, []);
  unsubscribe();
  assert.equal(f.listeners.size, 0);
});

test('other-tab login restores the new cookie session without rebroadcasting', async () => {
  const f = fixture({ native: false });
  await f.auth.signIn(credentials);
  const unsubscribe = f.auth.subscribeToChanges();
  f.receive('changed');
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(f.store.getSnapshot().user.id, 'rotated');
  assert.equal(f.state.refreshes, 1);
  assert.deepEqual(f.events, ['changed']);
  unsubscribe();
  f.receive('signed-out');
  assert.equal(f.store.getSnapshot().status, SessionStatus.Authenticated);
});

test('other-tab logout supersedes an earlier change event waiting for restoration', async () => {
  const f = fixture({ native: false });
  const gate = deferred();
  f.authApi.refresh = async () => { f.state.refreshes++; return gate.promise; };
  const restoring = f.auth.restoreSession();
  await new Promise(resolve => setImmediate(resolve));
  const unsubscribe = f.auth.subscribeToChanges();
  f.receive('changed');
  f.receive('signed-out');
  gate.resolve(response('obsolete'));
  await restoring;
  await new Promise(resolve => setImmediate(resolve));
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.equal(f.state.refreshes, 1);
  assert.deepEqual(f.events, []);
  unsubscribe();
});

test('sign-out during startup cannot restore the user from a late refresh', async () => {
  const f = fixture({ existing: 'old' });
  const gate = deferred();
  f.authApi.refresh = () => gate.promise;
  const restoring = f.auth.restoreSession();
  await new Promise(resolve => setImmediate(resolve));
  const logout = f.auth.signOut();
  gate.resolve(response('late'));
  await Promise.all([restoring, logout]);
  await f.auth.restoreSession();
  assert.equal(f.store.getSnapshot().status, SessionStatus.Anonymous);
  assert.deepEqual(f.state.revoked, ['refresh-late']);
  assert.equal(f.state.credential, null);
});

test('a failing auth operation does not prevent later login from using the queue', async () => {
  const f = fixture();
  f.authApi.login = async () => { throw new Error('Offline'); };
  await assert.rejects(f.auth.signIn(credentials));
  f.authApi.login = async () => response('recovered');
  await f.auth.signIn(credentials);
  assert.equal(f.store.getSnapshot().user.id, 'recovered');
});
