import type { IssuedSession } from "@/shared/session/session-api";

export type GoogleSignInResponse =
  | {
      status: "signedIn";
      session: IssuedSession;
    }
  | {
      status: "linkRequired" | "registrationRequired";
      continuationToken: string;
      continuationExpiresAtUtc: string;
    };
