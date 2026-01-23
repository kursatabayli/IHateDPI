/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      colors: {
        cyber: {
          dark: "#0a0a0f",
          darker: "#050508",
          primary: "#00f2ff",
          danger: "#ff0055",
          purple: "#a855f7",
          glass: "rgba(255, 255, 255, 0.05)",
          border: "rgba(255, 255, 255, 0.08)",
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      animation: {
        'glow': 'pulse-glow 3s infinite',
      },
      keyframes: {
        'pulse-glow': {
          '0%, 100%': { boxShadow: '0 0 20px rgba(0, 242, 255, 0.2)' },
          '50%': { boxShadow: '0 0 40px rgba(0, 242, 255, 0.5)' },
        }
      }
    },
  },
  plugins: [],
}