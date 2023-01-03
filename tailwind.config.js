/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,js}"],
  theme: {
    extend: {
      colors: {
        'primary-dark': '#b388eb',
        'primary-light': '#f7aef8',
        'secondary-dark': '#8093f1',
        'secondary-light': '#72ddf7',
        'tertiary': '#fdc5f5',
        'dark-1': '#00132d',
        'dark-2': '#00193b'
      }
    },
  },
  plugins: [],
}
