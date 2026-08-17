import { useState } from 'react';
import { Pressable, Text, View } from 'react-native';

import { Button } from '@/components/atoms/Button';
import { ChipSelector } from '@/components/molecules/ChipSelector';
import type { Alocacao } from '@/lib/api/alocacoes';
import type { AlunoProvisorio } from '@/lib/api/alunosProvisorios';
import type { Horario } from '@/lib/api/horarios';
import { NomesDiaSemana } from '@/lib/diaSemana';

export type HorarioAlocacaoCardProps = {
  horario: Horario;
  alunos: AlunoProvisorio[];
  alocacoes: Alocacao[];
  onAlocar: (horarioId: string, matriculaId: string) => void;
  onDesalocar: (horarioId: string, matriculaId: string) => void;
};

/**
 * Organismo: item da grade de alocação de Aluno em horário (issue #8).
 * Mostra vagas ocupadas/total, a lista de Alunos já alocados (com remover) e
 * um seletor dos Alunos ainda não alocados neste horário, para o Professor
 * escolher e confirmar com "Alocar" — não aloca ao selecionar o chip, só ao
 * confirmar, mesma separação seleção/confirmação de `HorarioForm`.
 */
export function HorarioAlocacaoCard({
  horario,
  alunos,
  alocacoes,
  onAlocar,
  onDesalocar,
}: HorarioAlocacaoCardProps) {
  const horaFormatada = horario.horaInicio.slice(0, 5);
  const { alocados, disponiveis } = separarAlunos(alunos, alocacoes);

  return (
    <View className="w-full gap-two rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <View>
        <Text className="text-base font-semibold text-text dark:text-dark-text">
          {NomesDiaSemana[horario.diaSemana]} · {horaFormatada}
        </Text>
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {alocacoes.length}/{horario.limiteAlunos}
        </Text>
      </View>
      {alocados.map((aluno) => (
        <AlunoAlocadoItem
          key={aluno.matriculaId}
          aluno={aluno}
          onRemover={() => onDesalocar(horario.id, aluno.matriculaId)}
        />
      ))}
      {disponiveis.length > 0 && (
        <SeletorAluno
          disponiveis={disponiveis}
          onAlocar={(matriculaId) => onAlocar(horario.id, matriculaId)}
        />
      )}
    </View>
  );
}

function separarAlunos(alunos: AlunoProvisorio[], alocacoes: Alocacao[]) {
  const idsAlocados = new Set(alocacoes.map((alocacao) => alocacao.matriculaId));
  const alocados = alunos.filter((aluno) => idsAlocados.has(aluno.matriculaId));
  const disponiveis = alunos.filter((aluno) => !idsAlocados.has(aluno.matriculaId));
  return { alocados, disponiveis };
}

function AlunoAlocadoItem({ aluno, onRemover }: { aluno: AlunoProvisorio; onRemover: () => void }) {
  return (
    <View className="flex-row items-center justify-between">
      <Text className="text-sm text-text dark:text-dark-text">{aluno.nome}</Text>
      <Pressable accessibilityRole="button" onPress={onRemover}>
        <Text className="text-sm font-semibold text-error dark:text-dark-error">Remover</Text>
      </Pressable>
    </View>
  );
}

function SeletorAluno({
  disponiveis,
  onAlocar,
}: {
  disponiveis: AlunoProvisorio[];
  onAlocar: (matriculaId: string) => void;
}) {
  const [selecionado, setSelecionado] = useState(disponiveis[0].matriculaId);
  const opcoes = disponiveis.map((aluno) => ({ valor: aluno.matriculaId, rotulo: aluno.nome }));

  return (
    <View className="gap-two">
      <ChipSelector label="Aluno" opcoes={opcoes} valor={selecionado} onChange={setSelecionado} />
      <Button label="Alocar" onPress={() => onAlocar(selecionado)} />
    </View>
  );
}
