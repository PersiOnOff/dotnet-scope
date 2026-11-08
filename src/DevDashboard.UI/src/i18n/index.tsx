import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { en, type Messages } from './en'
import { ru } from './ru'

export type { Messages } from './en'

export type Locale = 'en' | 'ru'

const STORAGE_KEY = 'dotnetscope.locale'

type I18n = {
  locale: Locale
  setLocale: (locale: Locale) => void
  t: Messages
}

const I18nContext = createContext<I18n>({ locale: 'en', setLocale: () => {}, t: en })

/**
 * Picks the browser language on first visit, then remembers the explicit choice. The dashboard
 * is a developer tool, so a Russian-speaking developer gets Russian without looking for a switch.
 */
function detectLocale(): Locale {
  try {
    const stored = window.localStorage.getItem(STORAGE_KEY)
    if (stored === 'en' || stored === 'ru') return stored
  } catch {
    // Storage can be unavailable; fall back to the browser language.
  }

  return navigator.language?.toLowerCase().startsWith('ru') ? 'ru' : 'en'
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocale] = useState<Locale>(detectLocale)

  const value = useMemo<I18n>(
    () => ({ locale, setLocale, t: locale === 'ru' ? ru : en }),
    [locale],
  )

  useEffect(() => {
    document.documentElement.lang = locale
    document.title = value.t.documentTitle
    try {
      window.localStorage.setItem(STORAGE_KEY, locale)
    } catch {
      // A preference that cannot be stored is not worth breaking the page over.
    }
  }, [locale, value.t.documentTitle])

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n(): I18n {
  return useContext(I18nContext)
}
