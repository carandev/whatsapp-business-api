/**
 * Marks `[data-reveal]` elements with `data-visible` once they enter the
 * viewport. Animations live in CSS (styles/tokens.css), so with
 * `prefers-reduced-motion` the content simply shows up.
 *
 * Safe to call repeatedly: elements already observed or visible are skipped,
 * which lets islands call it again after rendering new content.
 */
let observer: IntersectionObserver | null = null

function getObserver (): IntersectionObserver | null {
  if (!('IntersectionObserver' in window)) return null

  observer ??= new IntersectionObserver(entries => {
    for (const entry of entries) {
      if (entry.isIntersecting) {
        entry.target.setAttribute('data-visible', '')
        observer?.unobserve(entry.target)
      }
    }
  }, { rootMargin: '0px 0px -10% 0px', threshold: 0.1 })

  return observer
}

export function observeReveal (root: ParentNode = document) {
  const elements = root.querySelectorAll<HTMLElement>('[data-reveal]:not([data-visible]):not([data-reveal-observed])')
  const io = getObserver()

  elements.forEach(el => {
    if (!io) {
      el.setAttribute('data-visible', '')

      return
    }
    el.setAttribute('data-reveal-observed', '')
    io.observe(el)
  })
}
