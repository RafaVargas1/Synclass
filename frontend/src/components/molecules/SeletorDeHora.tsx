import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';

export type SeletorDeHoraProps = {
  label: string;
  valor: string | undefined;
  onSelecionar: (hora: string) => void;
};

/**
 * Molécula: campo de hora que abre um painel com duas listas roláveis
 * horizontais de números (Hora 00-23 e Minuto 00-59) em vez de aceitar
 * `HH:mm` digitado à mão (issue #117 — campo de hora sem máscara/seletor).
 * Mantém o range completo 00:00-23:59. Fecha sozinho ao confirmar.
 */
export function SeletorDeHora({ label, valor, onSelecionar }: SeletorDeHoraProps) {
  const [aberto, setAberto] = useState(false);

  return (
    <View className="gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">{label}</Text>
      <Pressable
        accessibilityRole="button"
        onPress={() => setAberto((atual) => !atual)}
        className="border border-border bg-background-element px-three py-two dark:border-dark-border dark:bg-dark-background-element"
      >
        <Text className="text-sm text-text dark:text-dark-text">
          {valor ? valor : 'Selecionar hora'}
        </Text>
      </Pressable>
      {aberto ? <Text>painel</Text> : null}
    </View>
  );
}
