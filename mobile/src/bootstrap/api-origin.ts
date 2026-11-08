export function resolveApiOrigin(configuredOrigin: string, browserOrigin?: string): string {
  if (browserOrigin !== 'http://localhost:8081' && browserOrigin !== 'http://127.0.0.1:8081') {
    return configuredOrigin;
  }

  if (!URL.canParse(configuredOrigin)) return configuredOrigin;
  const api = new URL(configuredOrigin);
  if (api.protocol !== 'http:' || (api.hostname !== 'localhost' && api.hostname !== '127.0.0.1')) {
    return configuredOrigin;
  }

  // Keep the web preview and the refresh cookie on the same site.
  api.hostname = new URL(browserOrigin).hostname;
  return api.origin;
}
