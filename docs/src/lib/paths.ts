/** Prefixes a site-relative path with the configured base (GitHub Pages serves under /whatsapp-business-api/). */
export function url (path = ''): string {
  const base = import.meta.env.BASE_URL.replace(/\/$/, '')
  const clean = path.replace(/^\//, '')
  return `${base}/${clean}`
}

export const repoUrl = 'https://github.com/carandev/whatsapp-business-api'
export const nugetUrl = 'https://www.nuget.org/packages/Carandev.WhatsApp.Sdk'
