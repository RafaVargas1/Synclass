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

/**
 * `deco` é a fonte de destaque do redesign art deco (títulos grandes,
 * marca) — só no web, mesma decisão já tomada para `sans`: nativo usa
 * fonte de sistema (aqui, o peso mais forte disponível) em vez de baixar
 * uma fonte extra, ver docs/spec/design-system.md#tipografia.
 */
export const Fonts = Platform.select({
  ios: {
    sans: 'system-ui',
    serif: 'ui-serif',
    rounded: 'ui-rounded',
    mono: 'ui-monospace',
    deco: 'system-ui',
  },
  default: {
    sans: 'normal',
    serif: 'serif',
    rounded: 'normal',
    mono: 'monospace',
    deco: 'sans-serif-condensed',
  },
  web: {
    sans: 'var(--font-display)',
    serif: 'var(--font-serif)',
    rounded: 'var(--font-rounded)',
    mono: 'var(--font-mono)',
    deco: 'var(--font-deco)',
  },
});

export const MaxContentWidth = 800;
