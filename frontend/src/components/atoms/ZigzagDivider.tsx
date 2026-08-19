import { useColorScheme, View, type ViewStyle } from 'react-native';

import { Colors } from '@/theme/tokens';

/**
 * `backgroundImage`/`backgroundRepeat`/`backgroundSize` são CSS válido no
 * RN Web, mas não fazem parte do tipo `ViewStyle` (pensado pro nativo) —
 * extensão local só pra essas três propriedades, em vez de `any` ou
 * silenciar o typecheck inteiro do bloco.
 */
type EstiloWeb = ViewStyle & {
  backgroundImage?: string;
  backgroundRepeat?: string;
  backgroundSize?: string;
};

const PadraoClaro = zigzagSvg(Colors.light.primary);
const PadraoEscuro = zigzagSvg(Colors.dark.primary);

function zigzagSvg(cor: string): string {
  const hex = cor.replace('#', '%23');
  return `url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='34' height='12' viewBox='0 0 34 12'%3E%3Cpath d='M0 12L8.5 0L17 12L25.5 0L34 12Z' fill='${hex}'/%3E%3C/svg%3E")`;
}

/**
 * Átomo decorativo: friso em zigue-zague (motivo art deco) usado sob os
 * headers no lugar de uma linha divisória lisa. Só aparece no web — RN
 * nativo não suporta `backgroundImage` de `View`, então nesse caso vira só
 * um espaçador (sem quebrar layout, sem tentar desenhar o padrão).
 */
export function ZigzagDivider() {
  const escuro = useColorScheme() === 'dark';
  const estilo: EstiloWeb = {
    backgroundImage: escuro ? PadraoEscuro : PadraoClaro,
    backgroundRepeat: 'repeat-x',
    backgroundSize: '34px 12px',
  };

  return <View className="h-3 w-full" style={estilo} />;
}
