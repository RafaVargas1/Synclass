import { useState } from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';

const Horas = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
const Minutos = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, '0'));
const HoraPadrao = '00';
const MinutoPadrao = '00';

export type SeletorDeHoraProps = {
  label: string;
  valor: string | undefined;
  onSelecionar: (hora: string) => void;
};

function desmembrarValor(valor: string | undefined): [string, string] {
  if (!valor) {
    return [HoraPadrao, MinutoPadrao];
  }
  const [hora, minuto] = valor.split(':');
  return [hora, minuto ?? MinutoPadrao];
}

/**
 * Molécula: campo de hora que abre um painel com duas listas roláveis
 * horizontais de números (Hora 00-23 e Minuto 00-59) em vez de aceitar
 * `HH:mm` digitado à mão (issue #117 — campo de hora sem máscara/seletor).
 * Mantém o range completo 00:00-23:59 e fecha ao confirmar a escolha.
 */
export function SeletorDeHora({ label, valor, onSelecionar }: SeletorDeHoraProps) {
  const [aberto, setAberto] = useState(false);
  const [horaSelecionada, setHoraSelecionada] = useState(() => desmembrarValor(valor)[0]);
  const [minutoSelecionado, setMinutoSelecionado] = useState(() => desmembrarValor(valor)[1]);

  function abrir() {
    const [hora, minuto] = desmembrarValor(valor);
    setHoraSelecionada(hora);
    setMinutoSelecionado(minuto);
    setAberto(true);
  }

  function confirmar() {
    onSelecionar(`${horaSelecionada}:${minutoSelecionado}`);
    setAberto(false);
  }

  return (
    <View className="gap-one">
      <Text className="text-sm font-medium text-text dark:text-dark-text">{label}</Text>
      <Pressable
        accessibilityRole="button"
        onPress={abrir}
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
                  accessibilityState={{ selected: hora === horaSelecionada }}
                  onPress={() => setHoraSelecionada(hora)}
                  className="items-center justify-center"
                  style={{ minWidth: 44, minHeight: 44 }}
                >
                  <Text
                    className={
                      hora === horaSelecionada
                        ? 'font-bold text-primary dark:text-dark-primary'
                        : 'text-sm text-text dark:text-dark-text'
                    }
                  >
                    {hora}
                  </Text>
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
                  accessibilityState={{ selected: minuto === minutoSelecionado }}
                  onPress={() => setMinutoSelecionado(minuto)}
                  className="items-center justify-center"
                  style={{ minWidth: 44, minHeight: 44 }}
                >
                  <Text
                    className={
                      minuto === minutoSelecionado
                        ? 'font-bold text-primary dark:text-dark-primary'
                        : 'text-sm text-text dark:text-dark-text'
                    }
                  >
                    {minuto}
                  </Text>
                </Pressable>
              ))}
            </View>
          </ScrollView>
          <Pressable
            accessibilityRole="button"
            onPress={confirmar}
            className="self-start border border-primary bg-primary px-three py-two dark:border-dark-primary dark:bg-dark-primary"
          >
            <Text className="font-medium text-white">Confirmar</Text>
          </Pressable>
        </View>
      ) : null}
    </View>
  );
}
