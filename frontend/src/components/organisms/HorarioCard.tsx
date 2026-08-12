import { Pressable, Text, View } from 'react-native';

import type { Horario } from '@/lib/api/horarios';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type HorarioCardProps = {
  horario: Horario;
  onRemover: (horarioId: string) => void;
};

/**
 * Organismo: item da lista de horários disponíveis do Professor (issue #6).
 * Mostra dia, hora de início e duração — sem opção de editar duração
 * (imutável após criação, ver Regra de Negócio da issue #6), só remover.
 */
export function HorarioCard({ horario, onRemover }: HorarioCardProps) {
  const horaFormatada = horario.horaInicio.slice(0, 5);

  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <View>
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {NomesDiaSemana[horario.diaSemana]} · {horaFormatada}
        </Text>
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {horario.duracaoMinutos} min
        </Text>
      </View>
      <Pressable accessibilityRole="button" onPress={() => onRemover(horario.id)}>
        <Text className="text-sm font-semibold text-error dark:text-dark-error">Remover</Text>
      </Pressable>
    </View>
  );
}
