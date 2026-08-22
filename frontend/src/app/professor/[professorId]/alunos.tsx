import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { AlunoVinculadoCard } from '@/components/organisms/AlunoVinculadoCard';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { listarAlocacoes, type Alocacao } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios } from '@/lib/api/alunosProvisorios';
import { listarHorarios, type Horario } from '@/lib/api/horarios';
import { agruparAlocacoesPorAluno } from '@/lib/agruparAlocacoesPorAluno';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela "Meus Alunos" (issue #160): visão geral de nome, identificador e
 * horário(s) de cada Aluno vinculado ao Professor — sem endpoint novo,
 * cruza `listarAlunosProvisorios`/`listarHorarios`/`listarAlocacoes` (uma
 * chamada por horário, mesmo padrão de `alocacoes.tsx`).
 */
export default function MeusAlunosScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const estado = useCarregamentoAlunos(professorId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Meus Alunos" />
      <View
        className="w-full flex-1 self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {estado.status === 'carregando' ? (
          <ActivityIndicator accessibilityLabel="Carregando" />
        ) : estado.status === 'falha' ? (
          <View className="flex-1 items-center justify-center gap-four">
            <ErrorMessage>{estado.mensagem}</ErrorMessage>
            <Button label="Tentar novamente" onPress={estado.tentarNovamente} />
          </View>
        ) : estado.alunos.length === 0 ? (
          <ErrorMessage>Nenhum Aluno cadastrado ainda.</ErrorMessage>
        ) : (
          <FlatList
            data={estado.alunos}
            keyExtractor={(item) => item.matriculaId}
            renderItem={({ item }) => <AlunoVinculadoCard aluno={item} />}
            contentContainerClassName="gap-two"
            style={{ minHeight: 420, flexGrow: 1 }}
          />
        )}
      </View>
    </SafeAreaView>
  );
}

type EstadoCarregamento =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string; tentarNovamente: () => void }
  | { status: 'carregado'; alunos: ReturnType<typeof agruparAlocacoesPorAluno> };

function useCarregamentoAlunos(professorId: string): EstadoCarregamento {
  const [resultado, setResultado] = useState<
    | { sucesso: true; alunos: ReturnType<typeof agruparAlocacoesPorAluno> }
    | { sucesso: false; mensagem: string }
    | undefined
  >(undefined);
  const [tentativa, setTentativa] = useState(0);
  const tentarNovamente = () => {
    setResultado(undefined);
    setTentativa((atual) => atual + 1);
  };

  useEffect(() => {
    let cancelado = false;
    carregarAlunosComHorarios(professorId).then((res) => {
      if (!cancelado) setResultado(res);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, tentativa]);

  if (resultado === undefined) return { status: 'carregando' };
  if (!resultado.sucesso) return { status: 'falha', mensagem: resultado.mensagem, tentarNovamente };
  return { status: 'carregado', alunos: resultado.alunos };
}

async function carregarAlunosComHorarios(professorId: string) {
  const [resultadoAlunos, resultadoHorarios] = await Promise.all([
    listarAlunosProvisorios(),
    listarHorarios(professorId),
  ]);
  if (!resultadoAlunos.sucesso) {
    return { sucesso: false as const, mensagem: resultadoAlunos.mensagem };
  }
  if (!resultadoHorarios.sucesso) {
    return { sucesso: false as const, mensagem: resultadoHorarios.mensagem };
  }
  const alocacoesPorHorario = await carregarAlocacoes(resultadoHorarios.horarios);
  return {
    sucesso: true as const,
    alunos: agruparAlocacoesPorAluno(resultadoAlunos.alunos, resultadoHorarios.horarios, alocacoesPorHorario),
  };
}

async function carregarAlocacoes(horarios: Horario[]): Promise<Record<string, Alocacao[]>> {
  const resultados = await Promise.all(horarios.map((horario) => listarAlocacoes(horario.id)));
  const entradas = horarios.map((horario, indice) => {
    const resultado = resultados[indice];
    return [horario.id, resultado.sucesso ? resultado.alocacoes : []] as const;
  });
  return Object.fromEntries(entradas);
}
