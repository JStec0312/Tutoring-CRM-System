import { Redirect } from "expo-router";

import { UnsupportedPortalScreen } from "@/features/portals/UnsupportedPortalScreen";
import { useAuth } from "@/shared/session/AuthProvider";
import { getPortalRoute } from "@/shared/session/portal-route";

export default function PortalIndex() {
  const { roles } = useAuth();
  const portal = getPortalRoute(roles);

  return portal ? <Redirect href={portal} /> : <UnsupportedPortalScreen />;
}
