/**
 * Ponto de entrada tipado dos tokens de design do Synclass para código RN.
 * Os valores em si vivem em ./palette.js (fonte única também consumida por
 * tailwind.config.js). Ver docs/spec/architecture.md#frontend-atomic-design.
 */
import { Platform, type ViewStyle } from 'react-native';

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

/**
 * Largura máxima do container de formulários (telas de cadastro/editar).
 * O Painel usa `MaxContentWidthPainel`, mais largo, por ser a tela pós-login
 * em telas grandes — ver docs/spec/design-system.md#largura-máxima-de-conteúdo.
 */
export const MaxContentWidth = 800;

/**
 * Largura máxima do container do Painel (issue #69): mais largo que
 * `MaxContentWidth` de formulário, porque o Painel distribui ações lado a
 * lado e sobra espaço em telas web largas. Nome explícito para não ser
 * confundido com o token genérico de formulário.
 */
export const MaxContentWidthPainel = 1120;

/**
 * Área de toque mínima (Fitts/WCAG 2.5.8/HIG/Material —
 * docs/spec/ux-heuristics.md#alvos-de-toque): 44×44pt, aplicada via
 * `style` (não `className`) em todo `Pressable` cujo desenho visual é
 * menor que isso. Constante única em vez do literal `{ minWidth: 44,
 * minHeight: 44 }` repetido em cada componente (achado de dev-review,
 * PR #122).
 */
export const AlvoDeToqueMinimo: ViewStyle = { minWidth: 44, minHeight: 44 };
