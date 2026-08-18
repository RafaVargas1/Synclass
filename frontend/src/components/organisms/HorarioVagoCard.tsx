import { Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import type { HorarioVago } from '@/lib/api/marcacoes';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type HorarioVagoCardProps = {
  horarioVago: HorarioVago;
  onMarcar: (horarioId: string) => void;
};

/**
 * Organismo: item da lista de horários vagos do Professor (issue #9). Mostra
 * dia, hora de início e vagas restantes (já calculadas pelo backend), com um
 * botão "Marcar" que confirma de imediato — sem passo de seleção prévia,
 * diferente de `HorarioAlocacaoCard` (issue #8), já que aqui o Aluno marca a
 * si mesmo, não escolhe entre vários. Mesma exibição de dia/hora de
 * `HorarioAlocacaoCard`/`HorarioCard` via `NomesDiaSemana` (sem duplicar
 * apresentação, ver docs/spec/code-style.md).
 */
export function HorarioVagoCard({ horarioVago, onMarcar }: HorarioVagoCardProps) {
  const horaFormatada = horarioVago.horaInicio.slice(0, 5);
  const rotuloVagas =
    horarioVago.vagasRestantes === 1 ? '1 vaga' : `${horarioVago.vagasRestantes} vagas`;

  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <View>
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {NomesDiaSemana[horarioVago.diaSemana]} · {horaFormatada}
        </Text>
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {rotuloVagas}
        </Text>
      </View>
      <Button label="Marcar" onPress={() => onMarcar(horarioVago.id)} />
    </View>
  );
}
