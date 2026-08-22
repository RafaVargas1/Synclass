import { Link } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import type { AlunoComHorarios } from '@/lib/agruparAlocacoesPorAluno';
import { NomesDiaSemana } from '@/lib/diaSemana';
import { AlvoDeToqueMinimo } from '@/theme/tokens';

export type AlunoVinculadoCardProps = { professorId: string; aluno: AlunoComHorarios };

/**
 * Organismo: card de um Aluno na tela "Meus Alunos" (issue #160) — nome,
 * identificador, os horários em que está alocado (ou "Sem horário" se
 * nenhum), e o link para a regra de cobrança da matrícula (issue #185 —
 * a tela já existia mas era inalcançável por navegação). Mesmo estilo
 * visual de card já usado em `HorarioCard`/`ValorDevidoCard`
 * (`rounded-medium border ... bg-background-element`), não inventar um
 * novo.
 */
export function AlunoVinculadoCard({ professorId, aluno }: AlunoVinculadoCardProps) {
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
      <Link href={`/professor/${professorId}/matriculas/${aluno.matriculaId}/regra-de-cobranca`} asChild>
        <Pressable
          accessibilityRole="button"
          className="mt-one items-start justify-center"
          style={AlvoDeToqueMinimo}
        >
          <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
            Regra de cobrança
          </Text>
        </Pressable>
      </Link>
    </View>
  );
}
