import { toByteArray } from 'base64-js';
import { AccessToken, InvalidSessionResponseError, Role, SessionError, User } from '../../../shared/session/session-types';

type SessionResult = { token: AccessToken; user: User; refreshToken: string | null };
const JwtClaim = { Subject: 'sub', Email: 'email', Role: 'role' } as const;
const TimeZone = { Utc: 'Z' } as const;
const TimestampWithZone = /(?:Z|[+-]\d{2}:\d{2})$/i;

function record(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== 'object' || Array.isArray(value)) throw new Error(SessionError.InvalidResponse);
  return value as Record<string, unknown>;
}

export function mapSession(response: unknown, native: boolean, now = Date.now()): SessionResult {
  try {
    const dto = record(response);
    if (typeof dto.accessToken !== 'string' || typeof dto.expiresAt !== 'string') throw new Error();
    // Existing backend DateTime values can omit their UTC suffix.
    const expiresAt = Date.parse(TimestampWithZone.test(dto.expiresAt) ? dto.expiresAt : dto.expiresAt + TimeZone.Utc);
    if (!Number.isFinite(expiresAt) || expiresAt <= now) throw new Error();
    const payload = dto.accessToken.split('.')[1];
    const encoded = payload.replace(/-/g, '+').replace(/_/g, '/');
    const decoded = toByteArray(encoded.padEnd(Math.ceil(encoded.length / 4) * 4, '='));
    const claims = record(JSON.parse(decodeURIComponent(Array.from(decoded, byte =>
      `%${byte.toString(16).padStart(2, '0')}`).join(''))));
    const id = claims[JwtClaim.Subject];
    const email = claims[JwtClaim.Email];
    const roleClaim = claims[JwtClaim.Role];
    const assigned = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
    const roles = Object.values(Role).filter(role => assigned.includes(role));
    if (typeof id !== 'string' || !id || typeof email !== 'string' || !roles.length) throw new Error();
    if (native && (typeof dto.refreshToken !== 'string' || !dto.refreshToken)) throw new Error();
    return {
      token: { value: dto.accessToken, expiresAt },
      user: { id, email, roles },
      refreshToken: native ? dto.refreshToken as string : null,
    };
  } catch {
    throw new InvalidSessionResponseError();
  }
}
