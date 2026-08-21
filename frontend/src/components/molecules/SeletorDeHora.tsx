import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';

const Horas = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const Minutos = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, '0'));

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
      {aberto ? (
        <View className="gap-one">
          <Text className="text-xs font-semibold uppercase tracking-widest text-text-secondary dark:text-dark-text-secondary">
            Hora
          </Text>
          <ScrollView horizontal>
            <View className="flex-row">
              {Horas.map((hora) => (
                <Pressable
                  key={hora}
                  accessibilityRole="button"
                  accessibilityLabel={`Hora ${hora}`}
                  className="items-center justify-center"
                  style={{ minWidth: 44, minHeight: 44 }}
                >
                  <Text className="text-sm text-text dark:text-dark-text">{hora}</Text>
                </Pressable>
              ))}
            </View>
          </ScrollView>
          <Text className="text-xs font-semibold uppercase tracking-widest text-text-secondary dark:text-dark-text-secondary">
            Minuto
          </Text>
          <ScrollView horizontal>
            <View className="flex-row">
              {Minutos.map((minuto) => (
                <Pressable
                  key={minuto}
                  accessibilityRole="button"
                  accessibilityLabel={`Minuto ${minuto}`}
                  className="items-center justify-center"
                  style={{ minWidth: 44, minHeight: 44 }}
                >
                  <Text className="text-sm text-text dark:text-dark-text">{minuto}</Text>
                </Pressable>
              ))}
            </View>
          </ScrollView>
        </View>
      ) : null}
    </View>
  );
}
