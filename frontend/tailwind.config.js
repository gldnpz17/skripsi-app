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
  safelist: [
    'bg-red-300',
    'bg-yellow-300',
    'bg-green-300',
    'bg-gray-300',
    'text-red-300',
    'text-yellow-300',
    'text-green-300',
    'text-gray-300',
    'text-white'
  ]
}
