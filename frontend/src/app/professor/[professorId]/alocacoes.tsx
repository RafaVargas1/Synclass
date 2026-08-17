import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { HorarioAlocacaoCard } from '@/components/organisms/HorarioAlocacaoCard';
import { alocarAluno, desalocarAluno, listarAlocacoes, type Alocacao } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios, type AlunoProvisorio } from '@/lib/api/alunosProvisorios';
import {
  ModeloAgendamento,
  obterConfiguracao,
  type ObterConfiguracaoResultado,
} from '@/lib/api/configuracao';
import { listarHorarios, type Horario } from '@/lib/api/horarios';

const MensagemModeloVagoTexto =
  'O modelo de agendamento Vago não usa atribuição fixa de Aluno a horário.';

/**
 * Tela de alocação de Aluno em horário (issue #8): gate de modelo de
 * agendamento na frente, igual a `horarios.tsx` (issue #7) — mas aqui, em
 * vez de exigir definir um modelo, bloqueia o conteúdo com uma mensagem
 * quando o modelo é Vago (ou ainda não definido), já que esse fluxo
 * pressupõe atribuição fixa (ver docs/specs/8-aluno-horario/implementation.md).
 * `professorId` vem da rota, mesma decisão de `horarios.tsx` (sem sessão
 * ainda, issue #18).
 */
export default function AlocacoesProfessorScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const carregamento = useCarregamentoConfiguracao(professorId);

  if (carregamento.status === 'carregando') {
    return <TelaCarregando />;
  }
  if (carregamento.status === 'falha') {
    return (
      <TelaErroConfiguracao
        mensagem={carregamento.mensagem}
        onTentarNovamente={carregamento.tentarNovamente}
      />
    );
  }
  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        <Heading level={1}>Alocação de Alunos</Heading>
        {permiteAlocacao(carregamento) ? (
          <AlocacoesConteudo professorId={professorId} />
        ) : (
          <ErrorMessage>{MensagemModeloVagoTexto}</ErrorMessage>
        )}
      </View>
    </SafeAreaView>
  );
}

type EstadoCarregamento =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string; tentarNovamente: () => void }
  | { status: 'carregada'; definida: boolean; modeloAgendamento?: ModeloAgendamento };

function permiteAlocacao(carregamento: Extract<EstadoCarregamento, { status: 'carregada' }>) {
  return carregamento.definida && carregamento.modeloAgendamento !== ModeloAgendamento.Vago;
}

/**
 * Mesma estratégia de `horarios.tsx#useCarregamentoConfiguracao` (achado do
 * dev-review/qa-review no PR #26, ver issue #1 Cenário 6): reage a falha de
 * rede com um estado próprio + retry, em vez de deixar a tela em branco.
 */
function useCarregamentoConfiguracao(professorId: string): EstadoCarregamento {
  const [resultado, setResultado] = useState<ObterConfiguracaoResultado | undefined>(undefined);
  const [tentativa, setTentativa] = useState(0);
  const tentarNovamente = () => {
    setResultado(undefined);
    setTentativa((atual) => atual + 1);
  };
  useEffect(() => {
    let cancelado = false;
    obterConfiguracao(professorId).then((res) => {
      if (!cancelado) setResultado(res);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, tentativa]);
  return paraEstadoCarregamento(resultado, tentarNovamente);
}

function paraEstadoCarregamento(
  resultado: ObterConfiguracaoResultado | undefined,
  tentarNovamente: () => void,
): EstadoCarregamento {
  if (resultado === undefined) {
    return { status: 'carregando' };
  }
  if (!resultado.sucesso) {
    return { status: 'falha', mensagem: resultado.mensagem, tentarNovamente };
  }
  if (!resultado.definida) {
    return { status: 'carregada', definida: false };
  }
  return { status: 'carregada', definida: true, modeloAgendamento: resultado.modeloAgendamento };
}

function TelaCarregando() {
  return (
    <SafeAreaView className="flex-1 items-center justify-center bg-background dark:bg-dark-background">
      <ActivityIndicator accessibilityLabel="Carregando" />
    </SafeAreaView>
  );
}

function TelaErroConfiguracao({
  mensagem,
  onTentarNovamente,
}: {
  mensagem: string;
  onTentarNovamente: () => void;
}) {
  return (
    <SafeAreaView className="flex-1 items-center justify-center gap-four bg-background px-four dark:bg-dark-background">
      <ErrorMessage>{mensagem}</ErrorMessage>
      <Button label="Tentar novamente" onPress={onTentarNovamente} />
    </SafeAreaView>
  );
}

function AlocacoesConteudo({ professorId }: { professorId: string }) {
  const estado = useGerenciamentoAlocacoes(professorId);
  return (
    <>
      {estado.erro ? <ErrorMessage>{estado.erro}</ErrorMessage> : null}
      <FlatList
        data={estado.horarios}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <HorarioAlocacaoCard
            horario={item}
            alunos={estado.alunos}
            alocacoes={estado.alocacoesPorHorario[item.id] ?? []}
            onAlocar={estado.handleAlocar}
            onDesalocar={estado.handleDesalocar}
          />
        )}
        contentContainerClassName="gap-two"
      />
    </>
  );
}

type AlocacoesPorHorario = Record<string, Alocacao[]>;

/**
 * Carrega horários, Alunos e alocações do Professor ao montar e expõe os
 * handlers de alocar/desalocar — extraído de `AlocacoesConteudo` no mesmo
 * padrão de `horarios.tsx#useGerenciamentoHorarios` (achado de tamanho de
 * função do dev-review, PR #26).
 */
function useGerenciamentoAlocacoes(professorId: string) {
  const [horarios, setHorarios] = useState<Horario[]>([]);
  const [alunos, setAlunos] = useState<AlunoProvisorio[]>([]);
  const [alocacoesPorHorario, setAlocacoesPorHorario] = useState<AlocacoesPorHorario>({});
  const [erro, setErro] = useState<string | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    carregarTudo(professorId).then((dados) => {
      if (!cancelado) {
        setHorarios(dados.horarios);
        setAlunos(dados.alunos);
        setAlocacoesPorHorario(dados.alocacoesPorHorario);
      }
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  const handleAlocar = criarHandleAlocar(professorId, setAlocacoesPorHorario, setErro);
  const handleDesalocar = criarHandleDesalocar(professorId, setAlocacoesPorHorario, setErro);

  return { horarios, alunos, alocacoesPorHorario, erro, handleAlocar, handleDesalocar };
}

async function carregarTudo(professorId: string) {
  const [resultadoHorarios, resultadoAlunos] = await Promise.all([
    listarHorarios(professorId),
    listarAlunosProvisorios(professorId),
  ]);
  const horarios = resultadoHorarios.sucesso ? resultadoHorarios.horarios : [];
  const alunos = resultadoAlunos.sucesso ? resultadoAlunos.alunos : [];
  const alocacoesPorHorario = await carregarAlocacoes(professorId, horarios);
  return { horarios, alunos, alocacoesPorHorario };
}

async function carregarAlocacoes(
  professorId: string,
  horarios: Horario[],
): Promise<AlocacoesPorHorario> {
  const resultados = await Promise.all(
    horarios.map((horario) => listarAlocacoes(professorId, horario.id)),
  );
  const entradas = horarios.map((horario, indice) => {
    const resultado = resultados[indice];
    return [horario.id, resultado.sucesso ? resultado.alocacoes : []] as const;
  });
  return Object.fromEntries(entradas);
}

function criarHandleAlocar(
  professorId: string,
  setAlocacoesPorHorario: (atualizador: (atual: AlocacoesPorHorario) => AlocacoesPorHorario) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async (horarioId: string, matriculaId: string) => {
    setErro(undefined);
    const resultado = await alocarAluno(professorId, horarioId, matriculaId);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setAlocacoesPorHorario((atual) => ({
      ...atual,
      [horarioId]: [...(atual[horarioId] ?? []), resultado.alocacao],
    }));
  };
}

function criarHandleDesalocar(
  professorId: string,
  setAlocacoesPorHorario: (atualizador: (atual: AlocacoesPorHorario) => AlocacoesPorHorario) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async (horarioId: string, matriculaId: string) => {
    setErro(undefined);
    const resultado = await desalocarAluno(professorId, horarioId, matriculaId);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setAlocacoesPorHorario((atual) => ({
      ...atual,
      [horarioId]: (atual[horarioId] ?? []).filter((alocacao) => alocacao.matriculaId !== matriculaId),
    }));
  };
}
