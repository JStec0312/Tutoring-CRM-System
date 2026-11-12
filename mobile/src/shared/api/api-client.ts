// This file contains the implementation of the API client for making HTTP requests.
// It includes request handling, error handling, and response parsing.
// It also checks for valid API URLs and handles authentication tokens.
// it hides the complexity of making authenticated API requests and reduces it to
// api.post(path, body, options)
import { ApiError, type ProblemDetails } from "./api-error";

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

type RequestOptions = {
  auth?: boolean;
  signal?: AbortSignal;
};

type ApiClientOptions = {
  baseUrl: string;
  credentials?: RequestCredentials;
  getAccessToken?: () => string | null | Promise<string | null>;
  fetcher?: typeof fetch;
};

export function createApiClient({
  baseUrl,
  credentials = "omit",
  getAccessToken,
  fetcher = fetch,
}: ApiClientOptions) {
  function resolveUrl(path: string): string {
    if (!baseUrl) {
      throw new Error("Missing EXPO_PUBLIC_API_URL");
    }

    if (!path.startsWith("/api/")) {
      throw new Error("API path must start with /api/");
    }

    const origin = new URL(baseUrl);
    const url = new URL(path, origin);

    if (url.origin !== origin.origin || !url.pathname.startsWith("/api/")) {
      throw new Error("Invalid API URL");
    }

    return url.toString();
  }

  async function request<T>(
    method: HttpMethod,
    path: string,
    body?: unknown,
    options: RequestOptions = {},
  ): Promise<T> {
    const url = resolveUrl(path);
    const headers = new Headers({
      Accept: "application/json",
    });

    if (body !== undefined) {
      headers.set("Content-Type", "application/json");
    }

    if (options.auth !== false) {
      const token = await getAccessToken?.();

      if (!token) {
        throw new ApiError(401, "Auth.SessionRequired");
      }

      headers.set("Authorization", `Bearer ${token}`);
    }

    let response: Response;

    try {
      response = await fetcher(url, {
        method,
        headers,
        credentials,
        signal: options.signal,
        body: body === undefined ? undefined : JSON.stringify(body),
      });
    } catch (error) {
      if (options.signal?.aborted) throw error;

      throw new ApiError(0, "Network.Unavailable");
    }

    let content: string;

    try {
      content = await response.text();
    } catch (error) {
      if (options.signal?.aborted) throw error;

      throw new ApiError(0, "Network.Unavailable");
    }

    if (!response.ok) {
      let problem: ProblemDetails | undefined;

      try {
        const parsed: unknown = JSON.parse(content);

        if (
          parsed !== null &&
          typeof parsed === "object" &&
          !Array.isArray(parsed)
        ) {
          problem = parsed as ProblemDetails;
        }
      } catch {
        // The server did not return valid ProblemDetails.
      }

      throw new ApiError(
        response.status,
        typeof problem?.code === "string"
          ? problem.code
          : `Http.${response.status}`,
        problem,
      );
    }

    if (!content || response.status === 204) {
      return undefined as T;
    }

    try {
      return JSON.parse(content) as T;
    } catch {
      throw new ApiError(response.status, "Response.InvalidJson");
    }
  }

  return {
    get: <T>(path: string, options?: RequestOptions) =>
      request<T>("GET", path, undefined, options),

    post: <T = void>(path: string, body?: unknown, options?: RequestOptions) =>
      request<T>("POST", path, body, options),

    put: <T = void>(path: string, body?: unknown, options?: RequestOptions) =>
      request<T>("PUT", path, body, options),

    patch: <T = void>(path: string, body?: unknown, options?: RequestOptions) =>
      request<T>("PATCH", path, body, options),

    delete: <T = void>(path: string, options?: RequestOptions) =>
      request<T>("DELETE", path, undefined, options),
  };
}

export type ApiClient = ReturnType<typeof createApiClient>;
