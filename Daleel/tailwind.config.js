/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'selector',
  content: [
    './Views/**/*.cshtml',
    './Pages/**/*.cshtml',
    './Controllers/**/*.cs',
    './Models/**/*.cs',
    './wwwroot/js/**/*.js',
    '../Daleel.BAL/**/*.cs',
    '../Daleel.DAL/**/*.cs'
  ],
  safelist: [
    // Dynamic badges in CMS Media library & CRM modules
    'bg-blue-500/10', 'text-blue-600', 'text-blue-400', 'border-blue-500/20',
    'bg-purple-500/10', 'text-purple-600', 'text-purple-400', 'border-purple-500/20',
    'bg-emerald-500/10', 'text-emerald-600', 'text-emerald-400', 'border-emerald-500/20',
    'bg-sky-500/10', 'text-sky-600', 'text-sky-400', 'border-sky-500/20',
    'bg-indigo-500/10', 'text-indigo-600', 'text-indigo-400', 'border-indigo-500/20',
    'bg-rose-500/10', 'text-rose-600', 'text-rose-400', 'border-rose-500/20',
    'bg-amber-500/10', 'text-amber-600', 'text-amber-400', 'border-amber-500/20',
    'bg-teal-500/10', 'text-teal-600', 'text-teal-400', 'border-teal-500/20',
    'bg-red-500/10', 'text-red-600', 'text-red-400', 'border-red-500/20'
  ],
  theme: {
    extend: {
      colors: {
        'surface-container-high': '#e6e8ea',
        'tertiary-fixed': '#ffdad3',
        'primary-fixed-dim': '#77d1ff',
        'error-container': '#ffdad6',
        'on-primary-fixed': '#001e2c',
        'surface-container-low': '#f2f4f6',
        'on-tertiary-fixed': '#3f0400',
        'on-surface': '#191c1e',
        'error': '#ba1a1a',
        'surface-tint': '#006689',
        'on-tertiary-container': '#790e00',
        'on-secondary-fixed-variant': '#31447a',
        'on-tertiary-fixed-variant': '#8e1300',
        'surface-variant': '#e0e3e5',
        'primary-fixed': '#c2e8ff',
        'inverse-on-surface': '#eff1f3',
        'surface-container-lowest': '#ffffff',
        'tertiary': '#ba1c00',
        'on-error-container': '#93000a',
        'on-secondary-container': '#3a4c83',
        'surface-bright': '#f7f9fb',
        'tertiary-fixed-dim': '#ffb4a5',
        'on-primary': '#ffffff',
        'on-secondary-fixed': '#00174b',
        'on-background': '#191c1e',
        'surface': '#f7f9fb',
        'secondary': '#4a5c94',
        'on-secondary': '#ffffff',
        'surface-container': '#eceef0',
        'secondary-fixed': '#dbe1ff',
        'secondary-container': '#acbffd',
        'on-primary-container': '#004058',
        'outline-variant': '#bdc8d0',
        'on-surface-variant': '#3d484f',
        'inverse-surface': '#2d3133',
        'primary': '#006689',
        'surface-dim': '#d8dadc',
        'on-tertiary': '#ffffff',
        'background': '#f7f9fb',
        'surface-container-highest': '#e0e3e5',
        'tertiary-container': '#ff8168',
        'secondary-fixed-dim': '#b4c5ff',
        'on-primary-fixed-variant': '#004d68',
        'outline': '#6d7980',
        'primary-container': '#00b2ec',
        'inverse-primary': '#77d1ff',
        'on-error': '#ffffff',
        // Daleel Brand Colors
        'daleel-navy': '#1D3166',
        'brand-navy': '#1D3166',
        'daleel-sky': '#00B2EC',
        'daleel-green': '#10B981',
        'daleel-orange': '#F9A01B',
        'daleel-offwhite': '#F8F9FA'
      },
      borderRadius: {
        'DEFAULT': '0.25rem',
        'lg': '0.5rem',
        'xl': '0.75rem',
        '2xl': '1rem',
        '3xl': '1.5rem',
        'full': '9999px'
      },
      spacing: {
        'gutter': '24px',
        'section-gap': '80px',
        'container-max': '1440px',
        'margin-page': '48px',
        'unit': '8px',
        'element-gap': '16px'
      },
      fontFamily: {
        'poppins': ['Poppins', 'sans-serif'],
        'inter': ['Inter', 'sans-serif'],
        'cairo': ['Cairo', 'sans-serif'],
        'body-lg': ['Inter', 'sans-serif'],
        'label-sm': ['Inter', 'sans-serif'],
        'body-md': ['Inter', 'sans-serif'],
        'h3': ['Poppins', 'sans-serif'],
        'h1': ['Poppins', 'sans-serif'],
        'h2': ['Poppins', 'sans-serif'],
        'button': ['Inter', 'sans-serif']
      },
      fontSize: {
        'body-lg': ['18px', { 'lineHeight': '1.6', 'letterSpacing': '0', 'fontWeight': '400' }],
        'label-sm': ['14px', { 'lineHeight': '1', 'letterSpacing': '0.02em', 'fontWeight': '500' }],
        'body-md': ['16px', { 'lineHeight': '1.6', 'letterSpacing': '0', 'fontWeight': '400' }],
        'h3': ['24px', { 'lineHeight': '1.4', 'letterSpacing': '0', 'fontWeight': '500' }],
        'h1': ['48px', { 'lineHeight': '1.2', 'letterSpacing': '-0.02em', 'fontWeight': '600' }],
        'h2': ['32px', { 'lineHeight': '1.3', 'letterSpacing': '-0.01em', 'fontWeight': '600' }],
        'button': ['16px', { 'lineHeight': '1', 'letterSpacing': '0.01em', 'fontWeight': '600' }]
      },
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'float': 'float 6s ease-in-out infinite'
      },
      keyframes: {
        'float': {
          '0%, 100%': { transform: 'translateY(0px)' },
          '50%': { transform: 'translateY(-10px)' }
        }
      }
    }
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
    require('@tailwindcss/container-queries')
  ]
};
