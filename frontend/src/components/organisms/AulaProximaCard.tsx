import { CheckCircle, X } from 'phosphor-react-native';
import { Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import type { AulaProxima } from '@/lib/api/cancelamentos';
import { formatarData } from '@/lib/formatarData';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type AulaProximaCardProps = {
  aulaProxima: AulaProxima;
  confirmado: boolean;
  onCancelar: (horarioId: string, data: string) => void;
  onConfirmar: (horarioId: string, data: string) => void;
};

const MensagemPrazoExpirado = 'Prazo para cancelar esta aula já passou.';
const MensagemPresencaConfirmada = 'Presença confirmada.';

/**
 * Organismo: item da lista de próximas aulas do Aluno (issue #10). Mostra
 * dia, hora de início e data da próxima ocorrência, com um botão "Cancelar"
 * — desabilitado com o motivo (prazo já expirado) quando o backend calcula
 * `podeCancelar === false`, mesma responsabilidade de cálculo já
 * centralizada em `AulaService` (não duplicada aqui) — e um botão
 * "Confirmar presença" (issue #15). `confirmado` é estado local da tela
 * (otimista, pós-200 da chamada), não um campo do contrato de
 * `GET proximas-aulas`: ver
 * docs/specs/15-aluno-confirma-presenca/implementation.md#decisão-de-implementação.
 * Mesma exibição de dia/hora de `HorarioVagoCard`/`HorarioAlocacaoCard` via
 * `NomesDiaSemana` (sem duplicar apresentação, ver
 * docs/spec/code-style.md).
 */
export function AulaProximaCard({ aulaProxima, confirmado, onCancelar, onConfirmar }: AulaProximaCardProps) {
  const horaFormatada = aulaProxima.horaInicio.slice(0, 5);
  const dataFormatada = formatarData(aulaProxima.data);
  const descricaoAula = `${NomesDiaSemana[aulaProxima.diaSemana]} ${horaFormatada} em ${dataFormatada}`;

  return (
    <View className="w-full flex-col gap-two rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element sm:flex-row sm:items-center sm:justify-between">
      <View className="sm:shrink">
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {NomesDiaSemana[aulaProxima.diaSemana]} · {horaFormatada}
        </Text>
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {dataFormatada}
        </Text>
        {!aulaProxima.podeCancelar ? (
          <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
            {MensagemPrazoExpirado}
          </Text>
        ) : null}
        {confirmado ? (
          <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
            {MensagemPresencaConfirmada}
          </Text>
        ) : null}
      </View>
      <View className="flex-row flex-wrap gap-two">
        {!confirmado ? (
          <Button
            label="Confirmar presença"
            icone={CheckCircle}
            testIDIcone="icone-acao-confirmar-presenca"
            accessibilityLabel={`Confirmar presença na aula de ${descricaoAula}`}
            onPress={() => onConfirmar(aulaProxima.horarioId, aulaProxima.data)}
          />
        ) : null}
        <Button
          label="Cancelar"
          icone={X}
          testIDIcone="icone-acao-cancelar"
          disabled={!aulaProxima.podeCancelar}
          accessibilityLabel={`Cancelar aula de ${descricaoAula}`}
          onPress={() => onCancelar(aulaProxima.horarioId, aulaProxima.data)}
        />
      </View>
    </View>
  );
}
