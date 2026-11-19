import { expect, it } from 'vitest';
import { resolveApiBaseUrl } from '@/shared/api/api-base-url';

it('keeps the LAN address for native clients', () => {
  expect(resolveApiBaseUrl('http://192.168.18.171:5106', 'http://localhost:5106'))
    .toBe('http://192.168.18.171:5106');
});

it('uses the web override instead of the phone address', () => {
  expect(resolveApiBaseUrl('http://192.168.18.171:5106', 'http://localhost:5106', 'http://localhost:8081'))
    .toBe('http://localhost:5106/');
});

it.each(['localhost', '127.0.0.1', '[::1]'])('matches local cookie host to %s preview', host => {
  expect(resolveApiBaseUrl('http://localhost:5106', undefined, `http://${host}:8081`))
    .toBe(`http://${host}:5106/`);
});

it('does not rewrite deployed HTTPS or remote API hosts', () => {
  expect(resolveApiBaseUrl('https://api.example.com', undefined, 'https://app.example.com'))
    .toBe('https://api.example.com/');
  expect(resolveApiBaseUrl('http://192.168.18.171:5106', undefined, 'http://localhost:8081'))
    .toBe('http://192.168.18.171:5106/');
});
