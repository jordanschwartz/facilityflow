/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#fff3ee',
          100: '#ffdecf',
          200: '#ffbc9e',
          300: '#ff9268',
          400: '#f76b3a',
          500: '#e8511a',
          600: '#d14815',
          700: '#b03a0f',
          800: '#8c2f0e',
          900: '#72280d',
        }
      }
    },
  },
  plugins: [],
}
