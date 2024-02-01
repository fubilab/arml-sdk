/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./{src,docs}/**/*.{js,jsx,ts,tsx,mdx}'],
  theme: {
    extend: {
      colors: {
        gray: {
          100: 'var(--ifm-color-emphasis-900)',
          200: 'var(--ifm-color-emphasis-800)',
          300: 'var(--ifm-color-emphasis-700)',
          400: 'var(--ifm-color-emphasis-600)',
          500: 'var(--ifm-color-emphasis-500)',
          600: 'var(--ifm-color-emphasis-400)',
          700: 'var(--ifm-color-emphasis-300)',
          800: 'var(--ifm-footer-background-color)',
          900: 'var(--ifm-color-emphasis-100)',
        },
        rose: {
          500: '#f66a68',
        },
        yellow: {
          500: '#f5ad65',
        },
        cyan: {
          500: '#4fded9',
        },
        sky: {
          500: '#6ccfee',
        },
        purple: {
          400: '#d794e2',
          500: '#d066e2',
        },
      },
      fontFamily: {
        sans: 'var(--ifm-font-family-base)',
      },
    },
  },
  plugins: [],
  corePlugins: {
    preflight: false,
  },
  darkMode: 'class',
}
