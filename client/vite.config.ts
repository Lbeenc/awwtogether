import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The ASP.NET API runs on :5158 in development (see server launchSettings.json).
const apiTarget = process.env.API_URL ?? 'http://localhost:5158'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/health': { target: apiTarget, changeOrigin: true },
    },
  },
  build: {
    // ASP.NET serves the built site from wwwroot in production.
    outDir: '../server/AwwTogether.Api/wwwroot',
    emptyOutDir: true,
  },
})
