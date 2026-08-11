/**
 * Ponto de entrada tipado dos tokens de design do Synclass para código RN.
 * Os valores em si vivem em ./palette.js (fonte única também consumida por
 * tailwind.config.js). Ver docs/spec/architecture.md#frontend-atomic-design.
 */
import { Platform } from 'react-native';

import palette from './palette.js';

export const Colors: {
  light: Record<string, string>;
  dark: Record<string, string>;
} = palette.Colors;

export type ThemeColor = keyof typeof Colors.light & keyof typeof Colors.dark;

export const Spacing: Record<string, number> = palette.Spacing;
export const Radius: Record<string, number> = palette.Radius;

export const Fonts = Platform.select({
  ios: {
    sans: 'system-ui',
    serif: 'ui-serif',
    rounded: 'ui-rounded',
    mono: 'ui-monospace',
  },
  default: {
    sans: 'normal',
    serif: 'serif',
    rounded: 'normal',
    mono: 'monospace',
  },
  web: {
    sans: 'var(--font-display)',
    serif: 'var(--font-serif)',
    rounded: 'var(--font-rounded)',
    mono: 'var(--font-mono)',
  },
});

export const MaxContentWidth = 800;
