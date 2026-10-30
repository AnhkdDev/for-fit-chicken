import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fixturesPlugin } from './tests/fixtures-plugin.ts';

const cert = resolve('.certs/localhost.pem');
const key = resolve('.certs/localhost.key');
export default defineConfig(({ mode, command }) => ({
  plugins: [react(), ...(command === 'serve' && mode === 'ui-fixtures' ? [fixturesPlugin()] : [])],
  server: {
    port: 5173,
    strictPort: true,
    fs: {
      deny: ['**/.env', '**/.env.*', '**/*.{crt,pem,key,pfx,p12}', '**/.certs/**', '**/.git/**'],
    },
    https:
      existsSync(cert) && existsSync(key)
        ? { cert: readFileSync(cert), key: readFileSync(key) }
        : undefined,
    proxy: { '/api': { target: 'https://localhost:7010', changeOrigin: false, secure: false } },
  },
}));
