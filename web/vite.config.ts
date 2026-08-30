import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: path.resolve(__dirname, '../SupplySmart.Host/wwwroot'),
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7091',
        changeOrigin: true,
        secure: false, // Disables SSL verification for local self-signed certificates
      },
    },
  },
})