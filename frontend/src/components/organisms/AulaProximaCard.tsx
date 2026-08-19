import { Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import type { AulaProxima } from '@/lib/api/cancelamentos';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type AulaProximaCardProps = {
  aulaProxima: AulaProxima;
  onCancelar: (horarioId: string, data: string) => void;
};

const MensagemPrazoExpirado = 'Prazo para cancelar esta aula já passou.';

/**
 * Organismo: item da lista de próximas aulas do Aluno (issue #10). Mostra
 * dia, hora de início e data da próxima ocorrência, com um botão "Cancelar"
 * — desabilitado com o motivo (prazo já expirado) quando o backend calcula
 * `podeCancelar === false`, mesma responsabilidade de cálculo já
 * centralizada em `AulaService` (não duplicada aqui). Mesma exibição de
 * dia/hora de `HorarioVagoCard`/`HorarioAlocacaoCard` via `NomesDiaSemana`
 * (sem duplicar apresentação, ver docs/spec/code-style.md).
 */
export function AulaProximaCard({ aulaProxima, onCancelar }: AulaProximaCardProps) {
  const horaFormatada = aulaProxima.horaInicio.slice(0, 5);

  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <View className="shrink">
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {NomesDiaSemana[aulaProxima.diaSemana]} · {horaFormatada}
        </Text>
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {aulaProxima.data}
        </Text>
        {!aulaProxima.podeCancelar ? (
          <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
            {MensagemPrazoExpirado}
          </Text>
        ) : null}
      </View>
      <Button
        label="Cancelar"
        disabled={!aulaProxima.podeCancelar}
        accessibilityLabel={`Cancelar aula de ${NomesDiaSemana[aulaProxima.diaSemana]} ${horaFormatada} em ${aulaProxima.data}`}
        onPress={() => onCancelar(aulaProxima.horarioId, aulaProxima.data)}
      />
    </View>
  );
}
