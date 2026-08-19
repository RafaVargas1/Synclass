import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';

import { CalendarioMensal } from '@/components/molecules/CalendarioMensal';
import { formatarData } from '@/lib/formatarData';

export type SeletorDeDataProps = {
  label: string;
  valor: string | undefined;
  onSelecionar: (dataISO: string) => void;
};

/**
 * Molécula: campo de data que abre um `CalendarioMensal` embutido em vez de
 * aceitar `yyyy-MM-dd` digitado à mão (issue de usabilidade — campo de data
 * sem máscara/seletor). Fecha sozinho ao escolher um dia.
 */
export function SeletorDeData({ label, valor, onSelecionar }: SeletorDeDataProps) {
  const [aberto, setAberto] = useState(false);
  const [mesReferencia, setMesReferencia] = useState(() => valor ? new Date(valor) : new Date());

  function selecionar(dataISO: string) {
    onSelecionar(dataISO);
    setAberto(false);
  }

  return (
    <View className="gap-one">
      <Text className="text-xs font-semibold uppercase tracking-widest text-text-secondary dark:text-dark-text-secondary">
        {label}
      </Text>
      <Pressable
        accessibilityRole="button"
        onPress={() => setAberto((atual) => !atual)}
        className="border border-border bg-background-element px-three py-two dark:border-dark-border dark:bg-dark-background-element"
      >
        <Text className="text-sm text-text dark:text-dark-text">
          {valor ? formatarData(valor) : 'Selecionar data'}
        </Text>
      </Pressable>
      {aberto ? (
        <CalendarioMensal
          mesReferencia={mesReferencia}
          dataSelecionada={valor}
          onSelecionarDia={selecionar}
          onMudarMes={setMesReferencia}
        />
      ) : null}
    </View>
  );
}
