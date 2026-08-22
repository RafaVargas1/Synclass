import { Text, View } from 'react-native';

import type { AlunoComHorarios } from '@/lib/agruparAlocacoesPorAluno';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type AlunoVinculadoCardProps = { aluno: AlunoComHorarios };

/**
 * Organismo: card de um Aluno na tela "Meus Alunos" (issue #160) — nome,
 * identificador, e os horários em que está alocado (ou "Sem horário" se
 * nenhum). Mesmo estilo visual de card já usado em `HorarioCard`/
 * `ValorDevidoCard` (`rounded-medium border ... bg-background-element`),
 * não inventar um novo.
 */
export function AlunoVinculadoCard({ aluno }: AlunoVinculadoCardProps) {
  return (
    <View className="w-full gap-one rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-base font-semibold text-text dark:text-dark-text">{aluno.nome}</Text>
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
        {aluno.identificador}
      </Text>
      <Text testID="horarios-do-aluno" className="text-sm text-text-secondary dark:text-dark-text-secondary">
        {aluno.horarios.length === 0
          ? 'Sem horário'
          : aluno.horarios
              .map((horario) => `${NomesDiaSemana[horario.diaSemana]} · ${horario.horaInicio.slice(0, 5)}`)
              .join(', ')}
      </Text>
    </View>
  );
}
