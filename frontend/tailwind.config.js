const { Colors, Spacing, Radius } = require('./src/theme/palette.js');

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}'],
  presets: [require('nativewind/preset')],
  theme: {
    extend: {
      colors: {
        text: Colors.light.text,
        background: Colors.light.background,
        'background-element': Colors.light.backgroundElement,
        'background-selected': Colors.light.backgroundSelected,
        'text-secondary': Colors.light.textSecondary,
        primary: Colors.light.primary,
        dark: {
          text: Colors.dark.text,
          background: Colors.dark.background,
          'background-element': Colors.dark.backgroundElement,
          'background-selected': Colors.dark.backgroundSelected,
          'text-secondary': Colors.dark.textSecondary,
          primary: Colors.dark.primary,
        },
      },
      spacing: {
        half: `${Spacing.half}px`,
        one: `${Spacing.one}px`,
        two: `${Spacing.two}px`,
        three: `${Spacing.three}px`,
        four: `${Spacing.four}px`,
        five: `${Spacing.five}px`,
        six: `${Spacing.six}px`,
      },
      borderRadius: {
        small: `${Radius.small}px`,
        medium: `${Radius.medium}px`,
        large: `${Radius.large}px`,
      },
    },
  },
  plugins: [],
};
