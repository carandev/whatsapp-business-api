import { defineConfig } from 'astro/config'

// Served from GitHub Pages at https://carandev.github.io/whatsapp-business-api/
export default defineConfig({
  site: 'https://carandev.github.io',
  base: '/whatsapp-business-api',
  trailingSlash: 'always',
  markdown: {
    shikiConfig: {
      themes: { light: 'vitesse-light', dark: 'vitesse-dark' },
      defaultColor: false
    }
  }
})
