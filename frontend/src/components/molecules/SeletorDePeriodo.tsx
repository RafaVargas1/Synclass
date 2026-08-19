import { View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { Input } from '@/components/atoms/Input';
import { Paragraph } from '@/components/atoms/Paragraph';

export type SeletorDePeriodoProps = {
  inicio: string;
  fim: string;
  onChangeInicio: (valor: string) => void;
  onChangeFim: (valor: string) => void;
  onConsultar: () => void;
};

/**
 * Molécula: par `Input` (início/fim) + `Button` para consultar um período
 * `yyyy-MM-dd` — extraída de `aluno/valor-devido.tsx` (issue #13) para ser
 * reaproveitada também por `aluno/historico-frequencia.tsx` (issue #16),
 * evitando duplicar o mesmo bloco (code-style.md).
 */
export function SeletorDePeriodo({ inicio, fim, onChangeInicio, onChangeFim, onConsultar }: SeletorDePeriodoProps) {
  return (
    <View className="gap-two">
      <Paragraph>Período (aaaa-mm-dd) — vazio usa o mês corrente</Paragraph>
      <View className="flex-row gap-two">
        <Input
          accessibilityLabel="Início do período"
          placeholder="Início"
          value={inicio}
          onChangeText={onChangeInicio}
          className="flex-1"
        />
        <Input
          accessibilityLabel="Fim do período"
          placeholder="Fim"
          value={fim}
          onChangeText={onChangeFim}
          className="flex-1"
        />
      </View>
      <Button label="Consultar" onPress={onConsultar} />
    </View>
  );
}
