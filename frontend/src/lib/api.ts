import { problemMessage } from './problem';
export class ApiError extends Error {
  constructor(
    public status: number,
    public code: string,
    message: string,
  ) {
    super(message);
  }
}
let csrf: string | undefined;
export const clearCsrf = () => {
  csrf = undefined;
};
export async function refreshCsrf() {
  const r = await fetch('/api/csrf', { credentials: 'same-origin', cache: 'no-store' });
  if (!r.ok) throw new Error('Không thể kết nối máy chủ. Vui lòng thử lại.');
  csrf = (await r.json()).requestToken as string;
  return csrf;
}
export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const method = options.method ?? 'GET';
  const headers = new Headers(options.headers);
  if (method !== 'GET' && method !== 'HEAD') {
    headers.set('X-CSRF-TOKEN', csrf ?? (await refreshCsrf()));
    if (options.body && !(options.body instanceof FormData))
      headers.set('Content-Type', 'application/json');
  }
  let r: Response;
  try {
    r = await fetch(`/api${path}`, {
      ...options,
      headers,
      credentials: 'same-origin',
      cache: 'no-store',
    });
  } catch {
    throw new Error('Chưa kết nối được máy chủ. Kiểm tra backend và kết nối mạng.');
  }
  if (!r.ok) {
    const p = await r.json().catch(() => ({}));
    if (r.status === 401 && !path.endsWith('/me') && !path.endsWith('/login'))
      window.dispatchEvent(new Event('session-expired'));
    if (p.code === 'CSRF_INVALID') clearCsrf();
    throw new ApiError(r.status, p.code ?? 'REQUEST_FAILED', problemMessage(p, r.status));
  }
  return r.status === 204 ? (undefined as T) : r.json();
}
export const send = <T>(path: string, method: string, data?: unknown, headers?: HeadersInit) =>
  api<T>(path, { method, headers, body: data === undefined ? undefined : JSON.stringify(data) });
export const money = (v: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(v);
export const date = (v?: string | null) =>
  v
    ? new Intl.DateTimeFormat('vi-VN', {
        dateStyle: 'short',
        timeStyle: 'short',
        timeZone: 'Asia/Ho_Chi_Minh',
      }).format(new Date(v))
    : 'Chưa có';
export function safeLink(v?: string | null, fallback = '/san-pham') {
  if (!v) return fallback;
  if (/^\/(?!\/)/.test(v) && !v.includes('\\')) return v;
  try {
    return new URL(v).protocol === 'https:' ? v : fallback;
  } catch {
    return fallback;
  }
}
