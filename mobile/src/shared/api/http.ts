export const HttpMethod = { Get: 'GET', Post: 'POST', Put: 'PUT', Delete: 'DELETE', Patch: 'PATCH' } as const;
export type HttpMethodValue = (typeof HttpMethod)[keyof typeof HttpMethod];
export const HttpStatus = { Unauthorized: 401, Forbidden: 403, NoContent: 204 } as const;
export const HttpHeader = { Authorization: 'Authorization', ContentType: 'Content-Type' } as const;
export const ContentType = { Json: 'application/json' } as const;
export const RequestCredentials = { Include: 'include', Omit: 'omit' } as const;
export const ApiMessage = {
  Failure: 'The request failed.',
  Network: 'Cannot connect to the server. Check your connection and try again.',
  Configuration: 'Set EXPO_PUBLIC_API_URL in mobile/.env.local before connecting to the API.',
  InvalidPath: 'Only relative backend API paths are allowed.',
} as const;
export const RequestTimeoutMs = 20_000;

export type ProblemDetails = { code?: string; detail?: string; title?: string; errors?: Record<string, string[]> };

export class ApiError extends Error {
  constructor(readonly status: number, readonly problem: ProblemDetails = {}) {
    super(problem.detail ?? problem.title ?? ApiMessage.Failure);
  }
}

export function isUnauthorized(error: unknown): error is ApiError {
  return error instanceof ApiError && error.status === HttpStatus.Unauthorized;
}

export const errorMessage = (error: unknown) => error instanceof Error ? error.message : ApiMessage.Failure;
