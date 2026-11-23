/** Turns "@handle" (or a full URL) into a profile link. */
export function handleUrl(network: 'instagram' | 'tiktok', handle: string): string {
  const h = handle.replace(/[[\]]/g, '').trim()
  if (/^https?:\/\//i.test(h)) return h
  const name = h.replace(/^@/, '')
  return network === 'instagram' ? `https://instagram.com/${name}` : `https://www.tiktok.com/@${name}`
}

/** Turns "example.com" into "https://example.com". */
export function siteUrl(site: string): string {
  const s = site.replace(/[[\]]/g, '').trim()
  return /^https?:\/\//i.test(s) ? s : `https://${s}`
}

/** Strips the [placeholder] brackets for display in links. */
export const clean = (s: string) => s.replace(/[[\]]/g, '')
