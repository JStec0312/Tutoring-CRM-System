import type { ApiClient } from "@/shared/api/api-client";

export type LoginCredentials = {
  email: string;
  password: string;
};

export type IssuedSession = {
  accessToken: string;
  expiresAt: string;
  refreshToken: string | null;
};

export type SessionApi = {
  login(credentials: LoginCredentials): Promise<IssuedSession>;
  refresh(refreshToken: string | null): Promise<IssuedSession>;
  signOut(refreshToken: string | null): Promise<void>;
};

export class InvalidSessionResponseError extends Error {
  readonly code = "Session.InvalidResponse";

  constructor() {
    super("Invalid session response from API");
    this.name = "InvalidSessionResponseError";
  }
}

type PlatformType = "web" | "native";

export function createSessionApi(
  api: Pick<ApiClient, "post">,
  platform: PlatformType,
): SessionApi {
  const isNative = platform === "native";

  const paths = isNative
    ? {
        login: "/api/auth/mobile/login",
        refresh: "/api/auth/mobile/refresh",
        signOut: "/api/auth/mobile/sign-out",
      }
    : {
        login: "/api/auth/login",
        refresh: "/api/auth/refresh",
        signOut: "/api/auth/sign-out",
      };

  return {
    async login(credentials) {
      const response = await api.post<unknown>(paths.login, credentials, {
        auth: false,
      });

      return parseSession(response, isNative);
    },

    async refresh(refreshToken) {
      if (isNative && !refreshToken) {
        throw new Error("Session.MissingRefreshToken");
      }

      const response = await api.post<unknown>(
        paths.refresh,
        isNative ? { refreshToken } : undefined,
        { auth: false },
      );

      return parseSession(response, isNative);
    },

    async signOut(refreshToken) {
      if (isNative && !refreshToken) {
        throw new Error("Session.MissingRefreshToken");
      }

      await api.post<void>(
        paths.signOut,
        isNative ? { refreshToken } : undefined,
        { auth: false },
      );
    },
  };
}

function parseSession(response: unknown, isNative: boolean): IssuedSession {
  if (
    typeof response !== "object" ||
    response === null ||
    Array.isArray(response)
  ) {
    throw new InvalidSessionResponseError();
  }

  const data = response as Record<string, unknown>;

  if (typeof data.accessToken !== "string" || !data.accessToken.trim()) {
    throw new InvalidSessionResponseError();
  }

  const expiresAt = parseUtcDate(data.expiresAt);

  let refreshToken: string | null = null;

  if (isNative) {
    if (typeof data.refreshToken !== "string" || !data.refreshToken.trim()) {
      throw new InvalidSessionResponseError();
    }

    // Mobile responses must include a valid refresh expiry.
    parseUtcDate(data.refreshTokenExpiresAt);

    refreshToken = data.refreshToken;
  }

  return {
    accessToken: data.accessToken,
    expiresAt,
    refreshToken,
  };
}

function parseUtcDate(value: unknown): string {
  if (
    typeof value !== "string" ||
    !/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?$/i.test(
      value,
    )
  ) {
    throw new InvalidSessionResponseError();
  }

  // Backend dates represent UTC. Handle dates with no offset.
  const utcValue = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : `${value}Z`;

  const timestamp = Date.parse(utcValue);

  if (!Number.isFinite(timestamp) || timestamp <= Date.now()) {
    throw new InvalidSessionResponseError();
  }

  return new Date(timestamp).toISOString();
}
