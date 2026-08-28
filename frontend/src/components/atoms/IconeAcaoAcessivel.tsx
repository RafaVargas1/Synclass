import type { Icon as IconeFosforo } from 'phosphor-react-native';
import { View } from 'react-native';

export type IconeAcaoAcessivelProps = {
  Icone: IconeFosforo;
  cor: string;
  testID: string;
};

/**
 * Ícone Phosphor (20px/regular, `docs/spec/design-system.md#ícones`) que
 * acompanha o rótulo de texto de uma ação (issue #202) — usado em
 * `Button`/`HorarioCard` sempre que um `Pressable`/`Button` já tem um
 * `label`/`Text` visível como nome acessível. `IconProps` do
 * phosphor-react-native não declara props de acessibilidade do RN
 * (`accessibilityElementsHidden`, `importantForAccessibility`) — a `View`
 * em volta é quem esconde o ícone da árvore de acessibilidade, evitando
 * duplicar o nome acessível.
 */
export function IconeAcaoAcessivel({ Icone, cor, testID }: IconeAcaoAcessivelProps) {
  return (
    <View testID={testID} accessibilityElementsHidden importantForAccessibility="no">
      <Icone size={20} weight="regular" color={cor} />
    </View>
  );
}
