import { useCallback, useEffect, useState } from 'react'

export type Theme = 'dark' | 'light'

const STORAGE_KEY = 'dotnetscope.theme'

/** Stored choice first, then the operating system preference. */
function detectTheme(): Theme {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY)
    if (stored === 'light' || stored === 'dark') return stored
  } catch {
    // Storage can be unavailable; the OS preference still works.
  }

  return window.matchMedia?.('(prefers-color-scheme: light)').matches ? 'light' : 'dark'
}

export function useTheme() {
  const [theme, setTheme] = useState<Theme>(detectTheme)

  useEffect(() => {
    document.documentElement.classList.toggle('light', theme === 'light')
    try {
      window.localStorage.setItem(STORAGE_KEY, theme)
    } catch {
      // A preference that cannot be stored is not worth breaking the page over.
    }
  }, [theme])

  const toggle = useCallback(() => setTheme(current => (current === 'dark' ? 'light' : 'dark')), [])

  return { theme, setTheme, toggle }
}
