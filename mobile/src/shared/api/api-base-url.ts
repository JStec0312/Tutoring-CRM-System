const LOOPBACK_HOSTS = ['localhost', '127.0.0.1', '[::1]'];

export function resolveApiBaseUrl(apiUrl: string, webApiUrl?: string, webOrigin?: string): string {
  if (!webOrigin) return apiUrl;

  const configuredUrl = webApiUrl || apiUrl;
  if (!configuredUrl) return configuredUrl;

  const api = new URL(configuredUrl);
  const web = new URL(webOrigin);
  // Local previews must use the same host for SameSite refresh cookies.
  if (api.protocol === 'http:' && web.protocol === 'http:' &&
      LOOPBACK_HOSTS.includes(api.hostname) && LOOPBACK_HOSTS.includes(web.hostname)) {
    api.hostname = web.hostname;
  }
  return api.toString();
}
