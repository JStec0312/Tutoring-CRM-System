// @vitest-environment jsdom
import { useState, type PropsWithChildren } from "react";
import { act, cleanup, fireEvent, render, renderHook, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";

const { replace, googleIdToken } = vi.hoisted(() => ({ replace: vi.fn(), googleIdToken: vi.fn() }));

vi.mock("react-native", () => vi.importActual("react-native-web"));
vi.mock("react-native-safe-area-context", async () => {
  const { View } = await vi.importActual<typeof import("react-native")>("react-native-web");
  return { SafeAreaView: View };
});
vi.mock("expo-router", () => ({
  router: { replace },
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => <a href={href}>{children}</a>,
}));
vi.mock("@/features/auth/sign-in/services/request-google-id-token", () => ({ requestGoogleIdToken: googleIdToken }));

import { SignInForm } from "@/features/auth/sign-in/components/SignInForm";
import { GoogleLinkScreen } from "@/features/auth/google-link/components/GoogleLinkScreen";
import { GoogleLinkProvider, useGoogleLink } from "@/features/auth/google-link/models/GoogleLinkProvider";
import { createApiClient } from "@/shared/api/api-client";
import { ApiProvider } from "@/shared/api/api-context";
import { AuthProvider } from "@/shared/session/AuthProvider";
import { createSessionApi } from "@/shared/session/session-api";
import { createSessionManager } from "@/shared/session/session-manager";

beforeEach(() => {
  vi.clearAllMocks();
  googleIdToken.mockResolvedValue("google-id-token");
});

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
  vi.useRealTimers();
});

async function setup(initialPage = "/sign-in", linkResponse?: () => Promise<Response>) {
  let currentEmail: string | null = null;
  const issued = () => ({
    accessToken: "header." + btoa(JSON.stringify({ sub: currentEmail, role: "Tutor" })) + ".signature",
    expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
  });
  const fetcher = vi.fn(async (url: RequestInfo | URL, options?: RequestInit) => {
    const path = new URL(String(url)).pathname;
    if (path === "/api/auth/google/sign-in") {
      return Response.json({
        status: "linkRequired", continuationToken: "a".repeat(64),
        continuationExpiresAtUtc: new Date(Date.now() + 300_000).toISOString(),
      });
    }
    if (path === "/api/auth/login") {
      const credentials = JSON.parse(String(options?.body));
      if (credentials.password !== "correct-password") {
        return Response.json({ code: "Auth.InvalidCredentials" }, { status: 401 });
      }
      currentEmail = credentials.email;
      return Response.json(issued());
    }
    if (path === "/api/auth/refresh") {
      if (!currentEmail) return Response.json({ code: "Auth.InvalidRefreshToken" }, { status: 401 });
      return Response.json(issued());
    }
    if (path === "/api/auth/sign-out") {
      currentEmail = null;
      return new Response(null, { status: 204 });
    }
    if (path === "/api/auth/google/link") {
      if (currentEmail !== "matching@example.com") {
        return Response.json({ code: "Auth.InvalidGoogleLinkContinuation" }, { status: 401 });
      }
      if (linkResponse) return linkResponse();
      return new Response(null, { status: 204 });
    }
    throw new Error("Unexpected API request: " + path);
  });
  const authHttp = createApiClient({ baseUrl: "https://example.com", credentials: "include", fetcher });
  const manager = createSessionManager({
    api: createSessionApi(authHttp, "web"),
    storage: {
      native: false, readRefreshToken: async () => null,
      writeRefreshToken: async () => {}, deleteRefreshToken: async () => {},
      isLogoutPending: async () => false, setLogoutPending: async () => {},
    },
  });
  const api = createApiClient({ baseUrl: "https://example.com", credentials: "include", session: manager, fetcher });

  function Pages() {
    const [page, setPage] = useState(initialPage);
    replace.mockImplementation(setPage);
    if (page === "/link-google") return <GoogleLinkScreen />;
    if (page === "/") return <p>Portal opened</p>;
    return <SignInForm />;
  }

  await act(async () => {
    render(
      <AuthProvider manager={manager}>
        <ApiProvider client={api}>
          <GoogleLinkProvider><Pages /></GoogleLinkProvider>
        </ApiProvider>
      </AuthProvider>,
    );
  });
  const linkCalls = () => fetcher.mock.calls.filter(([url]) => String(url).endsWith("/api/auth/google/link"));
  return { manager, fetcher, linkCalls };
}

async function startLinking() {
  fireEvent.click(screen.getByRole("button", { name: "Sign in with Google" }));
  await screen.findByRole("button", { name: "Sign in to continue" });
}

async function signIn(email = "matching@example.com", password = "correct-password") {
  fireEvent.change(screen.getByLabelText("Email address"), { target: { value: email } });
  fireEvent.change(screen.getByLabelText("Password"), { target: { value: password } });
  fireEvent.click(screen.getByRole("button", { name: "Sign in to continue" }));
  if (password === "correct-password") {
    const confirm = await screen.findByRole("button", { name: "Connect Google account" });
    await waitFor(() => expect(confirm.hasAttribute("disabled")).toBe(false));
  }
}

it("authenticates before confirmation and links only after an explicit click", async () => {
  const { manager, linkCalls } = await setup();
  await startLinking();
  expect(replace).toHaveBeenCalledWith("/link-google");
  expect(manager.getSnapshot().status).toBe("anonymous");
  await signIn();
  expect(manager.getSnapshot().status).toBe("authenticated");
  expect(linkCalls()).toHaveLength(0);
  expect(screen.queryByText("Portal opened")).toBeNull();
  fireEvent.click(screen.getByRole("button", { name: "Connect Google account" }));
  await screen.findByText("Portal opened");
  expect(linkCalls()).toHaveLength(1);
  expect(new Headers(linkCalls()[0][1]?.headers).get("Authorization")).toMatch(/^Bearer /);
});

it("keeps the continuation after a bad password and allows retrying", async () => {
  const { linkCalls } = await setup();
  await startLinking();
  await signIn("matching@example.com", "wrong-password");
  await screen.findByText("Invalid email address or password.");
  expect(linkCalls()).toHaveLength(0);
  await signIn();
  expect(screen.getByRole("button", { name: "Connect Google account" })).toBeTruthy();
});

it("cancels before authentication and clears the continuation", async () => {
  const { linkCalls } = await setup();
  await startLinking();
  fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
  await screen.findByRole("button", { name: "Sign in with Google" });
  act(() => replace("/link-google"));
  expect(screen.getByText(/expired or is no longer available/)).toBeTruthy();
  expect(linkCalls()).toHaveLength(0);
});

it("cancels after authentication while keeping the CRM session", async () => {
  const { manager, linkCalls } = await setup();
  await startLinking();
  await signIn();
  fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
  await screen.findByText("Portal opened");
  expect(manager.getSnapshot().status).toBe("authenticated");
  expect(linkCalls()).toHaveLength(0);
});

it("allows switching from a wrong CRM account without discarding the continuation", async () => {
  await setup();
  await startLinking();
  await signIn("wrong@example.com");
  fireEvent.click(screen.getByRole("button", { name: "Connect Google account" }));
  await screen.findByText(/If this attempt has expired/);
  fireEvent.click(screen.getByRole("button", { name: "Use a different CRM account" }));
  await screen.findByRole("button", { name: "Sign in to continue" });
  await signIn();
  fireEvent.click(screen.getByRole("button", { name: "Connect Google account" }));
  await screen.findByText("Portal opened");
});

it("prevents duplicate linking while the request is pending", async () => {
  const pending = Promise.withResolvers<Response>();
  const { linkCalls } = await setup("/sign-in", () => pending.promise);
  await startLinking();
  await signIn();
  const confirm = screen.getByRole("button", { name: "Connect Google account" });
  fireEvent.click(confirm);
  fireEvent.click(confirm);
  await waitFor(() => expect(linkCalls()).toHaveLength(1));
  await act(async () => pending.resolve(new Response(null, { status: 204 })));
  await screen.findByText("Portal opened");
});

it("does not link if the continuation expires before confirmation", async () => {
  const { linkCalls } = await setup();
  await startLinking();
  await signIn();
  const now = Date.now();
  vi.spyOn(Date, "now").mockReturnValue(now + 360_000);
  fireEvent.click(screen.getByRole("button", { name: "Connect Google account" }));
  await screen.findByText(/expired or is no longer available/);
  expect(linkCalls()).toHaveLength(0);
});

it("handles refreshing or opening the route without a continuation", async () => {
  const { linkCalls } = await setup("/link-google");
  expect(screen.getByText(/expired or is no longer available/)).toBeTruthy();
  expect(screen.queryByRole("button", { name: "Connect Google account" })).toBeNull();
  expect(linkCalls()).toHaveLength(0);
});

it("removes the in-memory continuation automatically at expiry", () => {
  vi.useFakeTimers();
  const wrapper = ({ children }: PropsWithChildren) => <GoogleLinkProvider>{children}</GoogleLinkProvider>;
  const { result } = renderHook(useGoogleLink, { wrapper });
  act(() => result.current.setContinuation({
    continuationToken: "a".repeat(64),
    continuationExpiresAtUtc: new Date(Date.now() + 1000).toISOString(),
  }));
  expect(result.current.continuation).not.toBeNull();
  act(() => vi.advanceTimersByTime(1001));
  expect(result.current.continuation).toBeNull();
});

it("keeps the linking screen available after a backend conflict and allows retrying", async () => {
  const response = vi.fn()
    .mockResolvedValueOnce(Response.json({ code: "Auth.GoogleAccountLinkConflict" }, { status: 409 }))
    .mockResolvedValueOnce(new Response(null, { status: 204 }));
  const { linkCalls } = await setup("/sign-in", response);
  await startLinking();
  await signIn();
  fireEvent.click(screen.getByRole("button", { name: "Connect Google account" }));
  await screen.findByText(/already connected to another account/);
  const confirm = screen.getByRole("button", { name: "Connect Google account" });
  await waitFor(() => expect(confirm.hasAttribute("disabled")).toBe(false));
  fireEvent.click(confirm);
  await screen.findByText("Portal opened");
  expect(linkCalls()).toHaveLength(2);
});

it("does not let an older expiry timer clear a newer continuation", () => {
  vi.useFakeTimers();
  const wrapper = ({ children }: PropsWithChildren) => <GoogleLinkProvider>{children}</GoogleLinkProvider>;
  const { result } = renderHook(useGoogleLink, { wrapper });
  act(() => result.current.setContinuation({
    continuationToken: "a".repeat(64),
    continuationExpiresAtUtc: new Date(Date.now() + 1000).toISOString(),
  }));
  act(() => vi.advanceTimersByTime(500));
  act(() => result.current.setContinuation({
    continuationToken: "b".repeat(64),
    continuationExpiresAtUtc: new Date(Date.now() + 3000).toISOString(),
  }));
  act(() => vi.advanceTimersByTime(600));
  expect(result.current.continuation?.continuationToken).toBe("b".repeat(64));
  act(() => vi.advanceTimersByTime(2401));
  expect(result.current.continuation).toBeNull();
});
