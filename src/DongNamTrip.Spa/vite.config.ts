import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ command }) => ({
  plugins: [react()],
  base: command === 'serve' ? '/' : '/spa/',
  server: {
    port: 5173,
    strictPort: true,
    fs: { allow: ['..'] },
    proxy: {
      '/spa-api': 'http://localhost:5081',
      '/images': 'http://localhost:5081',
    },
  },
  build: {
    outDir: '../DongNamTrip.Web/wwwroot/spa',
    emptyOutDir: true,
  },
}));
