import { useWindowDimensions } from 'react-native';

/** Breakpoint único de responsividade — viewport a partir daqui é "larga" (#77). */
export const LARGURA_TELA_DE_BREAKPOINT = 1024;

/**
 * Hook de responsividade (issue #77): diz se a viewport atual deve ser
 * tratada como larga (≈desktop). Usa `useWindowDimensions` e um breakpoint
 * fixo de 1024px, centralizado em `lib/` para telas futuras reutilizarem
 * sem duplicar o valor.
 */
export function useIsTelaLarga(): boolean {
  const { width } = useWindowDimensions();
  return width >= LARGURA_TELA_DE_BREAKPOINT;
}
