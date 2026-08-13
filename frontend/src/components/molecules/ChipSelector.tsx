import { Pressable, Text, View } from 'react-native';

export type ChipSelectorOption<T> = {
  valor: T;
  rotulo: string;
};

export type ChipSelectorProps<T> = {
  label: string;
  opcoes: readonly ChipSelectorOption<T>[];
  valor: T;
  onChange: (valor: T) => void;
};

/**
 * Molécula: seletor de uma opção entre várias, exibidas como chips
 * pressionáveis. Extraída de `HorarioForm` (issue #6, seletor de dia da
 * semana) ao criar `ModeloAgendamentoForm` (issue #7, seletor de modelo de
 * agendamento) para não duplicar o par View/Pressable com estado
 * selecionado/não selecionado entre os dois organismos (ver
 * docs/spec/code-style.md#estilo-de-código — "sem duplicação de código").
 */
export function ChipSelector<T>({ label, opcoes, valor, onChange }: ChipSelectorProps<T>) {
  return (
    <View className="w-full gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">{label}</Text>
      <View className="flex-row flex-wrap gap-one">
        {opcoes.map((opcao) => (
          <Chip
            key={opcao.rotulo}
            rotulo={opcao.rotulo}
            selecionado={opcao.valor === valor}
            onPress={() => onChange(opcao.valor)}
          />
        ))}
      </View>
    </View>
  );
}

type ChipProps = { rotulo: string; selecionado: boolean; onPress: () => void };

function Chip({ rotulo, selecionado, onPress }: ChipProps) {
  const corDeFundo = selecionado
    ? 'border-primary bg-primary dark:border-dark-primary dark:bg-dark-primary'
    : 'border-background-selected bg-background-element dark:border-dark-background-selected dark:bg-dark-background-element';
  const corDoTexto = selecionado ? 'text-white' : 'text-text dark:text-dark-text';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      onPress={onPress}
      className={`rounded-small border px-two py-one ${corDeFundo}`}
    >
      <Text className={corDoTexto}>{rotulo}</Text>
    </Pressable>
  );
}
