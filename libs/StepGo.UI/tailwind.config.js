/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "../../apps/**/*.{razor,html,cshtml}",
    "./**/*.{razor,html,cshtml}",
    "!**/bin/**",
    "!**/obj/**",
  ],
  theme: {
    // StepGo design tokens (design/design-handoff/README.md § Design Tokens).
    // Application code must reference these tokens (e.g. bg-stepgo-teacher) and
    // must never write hex literals — see frontend-design-system spec.
    colors: {
      transparent: "transparent",
      current: "currentColor",
      white: "#ffffff",
      stepgo: {
        bg: "#f7f4ee",
        card: "#fdfbf6",
        border: "#ded7c9",
        ink: "#22201c",
        "ink-muted": "#4a463d",
        "ink-subtle": "#6c6659",
        teacher: "#a7622c",
        "teacher-hover": "#7d461b",
        student: "#2f5560",
        "student-hover": "#24434c",
        danger: "#8a4b3f",
        "danger-hover": "#733d33",
        "admin-sidebar-bg": "#e3dcd1",
        "admin-sidebar-text": "#2b2822",
        "admin-sidebar-active": "#d2c6b4",
      },
    },
    fontFamily: {
      heading: ['"Noto Serif TC"', "serif"],
      sans: ['"Noto Sans TC"', "sans-serif"],
      number: ['"EB Garamond"', "serif"],
    },
    borderRadius: {
      none: "0",
      stepgo: "3px",
      "stepgo-lg": "4px",
      full: "9999px",
    },
    borderWidth: {
      DEFAULT: "1px",
      0: "0",
      2: "2px",
    },
    // Design tokens explicitly forbid shadows/gradients — no shadow utilities exist
    // other than "none", so accidental `shadow-md` etc. usage fails at build time.
    boxShadow: {
      none: "none",
    },
    // Two independent breakpoint sets (frontend-design-system spec):
    // system pages (sidebar layout: teacher/student/admin portals) collapse at
    // 1080px/980px; marketing pages reflow at 900px/640px. Kept in sync with the
    // Breakpoints C# constants in Components/Breakpoints.cs.
    screens: {
      "sys-wide": { min: "1080px" },
      "sys-mid": { min: "980px", max: "1079px" },
      "sys-narrow": { max: "979px" },
      "mkt-wide": { min: "900px" },
      "mkt-mid": { min: "640px", max: "899px" },
      "mkt-narrow": { max: "639px" },
    },
    extend: {},
  },
  plugins: [],
};
