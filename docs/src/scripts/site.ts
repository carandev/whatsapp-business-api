import { observeReveal } from './reveal'

function setupThemeToggle () {
  document.querySelectorAll<HTMLButtonElement>('[data-theme-toggle]').forEach(button => {
    button.addEventListener('click', () => {
      const next = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'
      document.documentElement.dataset.theme = next
      document.querySelector('meta[name="theme-color"]')?.setAttribute('content', next === 'dark' ? '#1A1513' : '#FAF7F2')
      try { localStorage.setItem('theme', next) } catch {}
    })
  })
}

function setupMenu () {
  const toggle = document.querySelector<HTMLButtonElement>('[data-menu-toggle]')
  const sidebar = document.querySelector<HTMLElement>('[data-sidebar]')
  if (!toggle || !sidebar) return
  toggle.addEventListener('click', () => {
    const open = !sidebar.hasAttribute('data-open')
    sidebar.toggleAttribute('data-open', open)
    toggle.setAttribute('aria-expanded', String(open))
  })
}

function setupCopyButtons () {
  document.querySelectorAll<HTMLPreElement>('.prose pre, .code-block pre').forEach(pre => {
    let wrapper = pre.parentElement
    if (!wrapper?.classList.contains('code-block')) {
      wrapper = document.createElement('div')
      wrapper.className = 'code-block'
      pre.replaceWith(wrapper)
      wrapper.append(pre)
    }
    if (wrapper.querySelector('.copy-btn')) return

    const button = document.createElement('button')
    button.type = 'button'
    button.className = 'copy-btn'
    button.textContent = 'Copy'
    button.addEventListener('click', async () => {
      try {
        await navigator.clipboard.writeText(pre.innerText)
        button.textContent = 'Copied'
        button.setAttribute('data-copied', '')
      } catch {
        button.textContent = 'Press Ctrl+C'
      }
      setTimeout(() => { button.textContent = 'Copy'; button.removeAttribute('data-copied') }, 1600)
    })
    wrapper.append(button)
  })
}

setupThemeToggle()
setupMenu()
setupCopyButtons()
observeReveal()
