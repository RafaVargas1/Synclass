import { Text, View } from 'react-native';

import type { AulaFrequenciaHistorico, StatusHistoricoFrequencia } from '@/lib/api/historicoFrequencia';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type HistoricoFrequenciaCardProps = {
  aula: AulaFrequenciaHistorico;
};

const RotulosPorStatus: Record<StatusHistoricoFrequencia, string> = {
  NaoRegistrada: 'Não registrada',
  Presente: 'Presente',
  Ausente: 'Ausente',
  Cancelada: 'Cancelada',
};

/**
 * Cor do texto do status por `StatusHistoricoFrequencia` (issue #16) — só as
 * cores já definidas em `tailwind.config.js` (`primary`/`error`/
 * `text-secondary`), sem introduzir um token novo para este card.
 */
const CoresPorStatus: Record<StatusHistoricoFrequencia, string> = {
  NaoRegistrada: 'text-text-secondary dark:text-dark-text-secondary',
  Presente: 'text-primary dark:text-dark-primary',
  Ausente: 'text-error dark:text-dark-error',
  Cancelada: 'text-text-secondary dark:text-dark-text-secondary',
};

/**
 * Organismo: item da lista de histórico de frequência do Aluno (issue #16) —
 * dia da semana, data, hora de início e um indicador visual (cor + texto)
 * por `StatusHistoricoFrequencia`. Mesmo padrão visual de `HorarioCard`.
 */
export function HistoricoFrequenciaCard({ aula }: HistoricoFrequenciaCardProps) {
  const horaFormatada = aula.horaInicio.slice(0, 5);

  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <View>
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {NomesDiaSemana[aula.diaSemana]} · {formatarData(aula.data)}
        </Text>
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">{horaFormatada}</Text>
      </View>
      <Text className={`text-sm font-semibold ${CoresPorStatus[aula.status]}`}>{RotulosPorStatus[aula.status]}</Text>
    </View>
  );
}

function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-');
  return `${dia}/${mes}/${ano}`;
}
