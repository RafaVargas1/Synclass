import { Link } from 'expo-router';
import { Pressable, Text, View } from 'react-native';

import type { VinculoProfessor } from '@/lib/api/vinculosAluno';
import { AlvoDeToqueMinimo } from '@/theme/tokens';

export type ProfessorVinculadoCardProps = { professor: VinculoProfessor };

/**
 * Organismo: card de um Professor vinculado na tela "Meus Professores"
 * (issue #182) — nome + duas ações (ver horários disponíveis, ver minhas
 * aulas), mesmo estilo visual de card já usado em `AlunoVinculadoCard`
 * (`rounded-medium border ... bg-background-element`).
 */
export function ProfessorVinculadoCard({ professor }: ProfessorVinculadoCardProps) {
  return (
    <View className="w-full gap-two rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-base font-semibold text-text dark:text-dark-text">{professor.nome}</Text>
      <View className="flex-row flex-wrap gap-two">
        <Link href={`/aluno/professores/${professor.professorId}/minhas-aulas`} asChild>
          <Pressable
            className="items-center justify-center rounded-small bg-primary px-three dark:bg-dark-primary"
            style={AlvoDeToqueMinimo}
          >
            <Text className="font-medium text-background dark:text-dark-background">Minhas aulas</Text>
          </Pressable>
        </Link>
        <Link href={`/aluno/professores/${professor.professorId}/horarios`} asChild>
          <Pressable
            className="items-center justify-center rounded-small border border-primary px-three dark:border-dark-primary"
            style={AlvoDeToqueMinimo}
          >
            <Text className="font-medium text-primary dark:text-dark-primary">Horários disponíveis</Text>
          </Pressable>
        </Link>
      </View>
    </View>
  );
}
