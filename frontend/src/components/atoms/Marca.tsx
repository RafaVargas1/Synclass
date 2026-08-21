import { View } from 'react-native';

export type MarcaProps = {
  /** Multiplicador aplicado às alturas base das 5 barras. Default 1. */
  escala?: number;
  invertido?: boolean;
};

const AlturasBase = [12, 24, 16, 28, 18];

/**
 * Átomo de marca: 5 barras de altura irregular, estilo "gráfico de
 * presença/barras" (issue #79) — substitui a coroa simétrica anterior
 * (`Crown.tsx`) por um símbolo que remete à função do produto (controle
 * de presença). Padrão fixo e assimétrico — só `escala`/`invertido`
 * variam, não as alturas relativas, pra manter o símbolo reconhecível em
 * qualquer tamanho de uso.
 */
export function Marca({ escala = 1, invertido = false }: MarcaProps) {
  return (
    <View
      testID="marca"
      className="flex-row items-end gap-one"
      style={invertido ? { transform: [{ scaleY: -1 }] } : undefined}
    >
      {AlturasBase.map((altura, indice) => (
        <View
          key={indice}
          testID={`marca-barra-${indice}`}
          className="w-1.5 bg-primary dark:bg-dark-primary"
          style={{ height: altura * escala }}
        />
      ))}
    </View>
  );
}
