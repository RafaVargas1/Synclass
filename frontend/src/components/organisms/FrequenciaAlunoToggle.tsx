import { Text, View } from 'react-native';

import { ChipSelector } from '@/components/molecules/ChipSelector';
import type { AlunoProvisorio } from '@/lib/api/alunosProvisorios';

export type FrequenciaAlunoToggleProps = {
  aluno: AlunoProvisorio;
  presente: boolean;
  onChange: (matriculaId: string, presente: boolean) => void;
};

const Opcoes = [
  { valor: true, rotulo: 'Presente' },
  { valor: false, rotulo: 'Ausente' },
] as const;

/**
 * Organismo: item da chamada do Professor (issue #14) — nome do Aluno e um
 * `ChipSelector` de 2 opções (Presente/Ausente) por Aluno, reaproveitando a
 * mesma molécula de seleção única já usada por
 * `HorarioAlocacaoCard`/`ModeloAgendamentoForm` em vez de um componente de
 * toggle novo (ver docs/spec/code-style.md — "sem duplicação de código").
 */
export function FrequenciaAlunoToggle({ aluno, presente, onChange }: FrequenciaAlunoToggleProps) {
  return (
    <View className="w-full gap-two rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-base font-semibold text-text dark:text-dark-text">{aluno.nome}</Text>
      <ChipSelector
        label="Frequência"
        opcoes={Opcoes}
        valor={presente}
        onChange={(valor) => onChange(aluno.matriculaId, valor)}
      />
    </View>
  );
}
