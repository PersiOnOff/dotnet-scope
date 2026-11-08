/** @type {import('tailwindcss').Config} */

// Every colour used by the UI is a CSS variable, so switching the theme is a class on <html>
// instead of a second set of classes in every component. `<alpha-value>` keeps the `/40`
// opacity modifiers working.
const token = name => `rgb(var(--${name}) / <alpha-value>)`

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // The "slate" scale doubles as the surface/ink scale: 950 is the page, 900 is a raised
        // panel, 800 is a line, 200..100 is text. The dark and light palettes swap the values.
        slate: {
          100: token('slate-100'),
          200: token('slate-200'),
          300: token('slate-300'),
          400: token('slate-400'),
          500: token('slate-500'),
          600: token('slate-600'),
          700: token('slate-700'),
          800: token('slate-800'),
          900: token('slate-900'),
          950: token('slate-950'),
        },
        indigo: {
          300: token('indigo-300'),
          400: token('indigo-400'),
          500: token('indigo-500'),
        },
        violet: {
          300: token('violet-300'),
          400: token('violet-400'),
          500: token('violet-500'),
        },
        sky: {
          300: token('sky-300'),
          400: token('sky-400'),
          500: token('sky-500'),
        },
        emerald: {
          300: token('emerald-300'),
          400: token('emerald-400'),
        },
        amber: {
          300: token('amber-300'),
          400: token('amber-400'),
          500: token('amber-500'),
        },
        rose: {
          200: token('rose-200'),
          300: token('rose-300'),
          400: token('rose-400'),
          500: token('rose-500'),
        },
      },
    },
  },
  plugins: [],
}
