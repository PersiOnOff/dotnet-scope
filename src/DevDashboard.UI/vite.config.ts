import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
export default defineConfig({
  plugins: [react()],
  base: '/_dashboard/',
  build: { outDir: '../DevDashboard.Core/wwwroot', emptyOutDir: true },
  server: { proxy: { '/_dashboard/api': 'http://localhost:5000', '/_dashboard/hub': { target: 'http://localhost:5000', ws: true } } },
})
