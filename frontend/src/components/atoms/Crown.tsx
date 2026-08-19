import { View } from 'react-native';

export type CrownProps = {
  /** Alturas dos degraus, do canto pra dentro — ex: `[8, 16, 24]` cresce até o centro. */
  degraus: number[];
  invertido?: boolean;
};

/**
 * Átomo decorativo: coroa escalonada (motivo art deco, ver
 * docs/spec/design-system.md#tipografia) usada como ornamento estrutural
 * no lugar de sombra/gradiente. `degraus` descreve metade do desenho — o
 * componente espelha para formar o padrão simétrico completo.
 */
export function Crown({ degraus, invertido = false }: CrownProps) {
  const alturas = [...degraus, ...[...degraus].reverse().slice(1)];

  return (
    <View
      className="flex-row items-end gap-one"
      style={invertido ? { transform: [{ scaleY: -1 }] } : undefined}
    >
      {alturas.map((altura, indice) => (
        <View key={indice} className="w-1.5 bg-primary dark:bg-dark-primary" style={{ height: altura }} />
      ))}
    </View>
  );
}
