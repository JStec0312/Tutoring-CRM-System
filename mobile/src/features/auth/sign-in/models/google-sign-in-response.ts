import type { IssuedSession } from "@/shared/session/session-api";
import type { GoogleContinuation } from "./google-continuation";

export type GoogleSignInResponse =
  | {
      status: "signedIn";
      session: IssuedSession;
    }
  | GoogleContinuation;
