import { url } from './paths'

export interface ParamDoc { Name: string; Text: string | null }
export interface MemberDoc {
  Kind: 'constructor' | 'property' | 'method' | 'field'
  Name: string
  Signature: string
  Summary: string | null
  Remarks: string | null
  Returns: string | null
  Params: ParamDoc[]
  Exceptions: ParamDoc[]
}
export interface TypeDoc {
  Namespace: string
  Name: string
  Slug: string
  Kind: string
  Declaration: string
  Summary: string | null
  Remarks: string | null
  Members: MemberDoc[]
}
export interface NamespaceDoc { name: string; types: TypeDoc[] }

const generated = import.meta.glob<{ namespaces: NamespaceDoc[] }>('../generated/api.json', { eager: true, import: 'default' })

/** Loads the API model produced by tools/ApiExtractor (`npm run api`). */
export function loadApi (): NamespaceDoc[] {
  const model = generated['../generated/api.json']
  if (!model) {
    throw new Error('src/generated/api.json is missing. Run `npm run api` (requires the .NET SDK) before building the docs.')
  }
  return model.namespaces
}

const slugs = new Set(loadApi().flatMap(ns => ns.types.map(t => t.Slug)))

export const typeUrl = (slug: string) => url(`api/${slug}/`)
export const namespaceSlug = (name: string) => name.toLowerCase().replaceAll('.', '-')

/** Rewrites `<a data-type="...">` cross-references emitted by the extractor into links (or plain code when unknown). */
export function linkify (html: string | null): string {
  if (!html) return ''
  return html.replace(/<a data-type="([^"]+)">(.*?)<\/a>/g, (_, slug: string, label: string) =>
    slugs.has(slug) ? `<a href="${typeUrl(slug)}">${label}</a>` : label)
}

/** Strips tags for plain-text contexts such as meta descriptions. */
export const plain = (html: string | null) => (html ?? '').replace(/<[^>]+>/g, '').replace(/&#39;/g, "'").replace(/&quot;/g, '"').replace(/&amp;/g, '&')

export const shortNamespace = (name: string) => name.replace(/^Carandev\.WhatsApp\.Sdk\.?/, '') || 'Carandev.WhatsApp.Sdk'
