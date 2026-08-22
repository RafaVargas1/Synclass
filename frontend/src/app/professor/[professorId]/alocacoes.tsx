import { Link, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { HorarioAlocacaoCard } from '@/components/organisms/HorarioAlocacaoCard';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { alocarAluno, desalocarAluno, listarAlocacoes, type Alocacao } from '@/lib/api/alocacoes';
import { listarAlunosProvisorios, type AlunoProvisorio } from '@/lib/api/alunosProvisorios';
import { ModeloAgendamento, obterConfiguracao } from '@/lib/api/configuracao';
import { listarHorarios, type Horario } from '@/lib/api/horarios';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de alocação de Aluno em horário (issue #8). Desde a issue #139 o
 * gate de modelo é substituído por uma decisão de estado
 * (`resolverEstadoAlocacao`) que combina a situação da configuração, a
 * existência de horários e (no caso de modelo Livre) a presença de alguma
 * alocação — cada cenário mostra a mensagem com a causa raiz, em vez de
 * uma única frase sobre "modelo" (ver docs/specs/139-mensagem-alocacao-clara/implementation.md).
 * `professorId` continua vindo da rota (issue #23 não muda `horarios.tsx`/
 * `HorariosController` nem `configuracao.ts`/`ConfiguracoesController`,
 * chamados aqui também — fora de escopo).
 */
export default function AlocacoesProfessorScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const estado = useEstadoAlocacao(professorId);

  if (estado.tipo === 'carregando') {
    return <TelaCarregando />;
  }
  if (estado.tipo === 'falha') {
    return (
      <TelaErroConfiguracao
        mensagem={estado.mensagem}
        onTentarNovamente={estado.tentarNovamente}
      />
    );
  }
  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Alocação de Alunos" />
      <View
        className="w-full flex-1 self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {estado.tipo === 'permite-alocacao' ? (
          <AlocacoesConteudo professorId={professorId} />
        ) : (
          <MensagemDoEstado estado={estado} professorId={professorId} />
        )}
      </View>
    </SafeAreaView>
  );
}

type EstadoCarregamento =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string; tentarNovamente: () => void }
  | { status: 'carregada'; definida: boolean; modeloAgendamento?: ModeloAgendamento };

type EstadoAlocacaoResolvido =
  | { tipo: 'sem-horarios' }
  | { tipo: 'modelo-nao-configurado' }
  | { tipo: 'modelo-livre'; temAlunosInscritos: boolean }
  | { tipo: 'permite-alocacao' };

type EstadoAlocacao =
  | { tipo: 'carregando' }
  | { tipo: 'falha'; mensagem: string; tentarNovamente: () => void }
  | EstadoAlocacaoResolvido;

/**
 * Decisão de estado da tela (issue #139): a ordem importa — sem horários
 * cadastrados sempre aparece primeiro (é a causa raiz mais frequente),
 * depois modelo não configurado, depois modelo Livre explicado à parte;
 * qualquer modelo Fixo/Híbrido libera a grade de alocação como antes.
 */
function resolverEstadoAlocacao(
  carregamento: Extract<EstadoCarregamento, { status: 'carregada' }>,
  horarios: Horario[],
  totalAlocacoes: number,
): EstadoAlocacaoResolvido {
  if (horarios.length === 0) {
    return { tipo: 'sem-horarios' };
  }
  if (!carregamento.definida) {
    return { tipo: 'modelo-nao-configurado' };
  }
  if (carregamento.modeloAgendamento === ModeloAgendamento.Vago) {
    return { tipo: 'modelo-livre', temAlunosInscritos: totalAlocacoes > 0 };
  }
  return { tipo: 'permite-alocacao' };
}

type CarregarEstadoResultado =
  | { sucesso: true; estado: EstadoAlocacaoResolvido }
  | { sucesso: false; mensagem: string };

/**
 * Mesma estratégia de `horarios.tsx#useCarregamentoConfiguracao` (achado do
 * dev-review/qa-review no PR #26, ver issue #1 Cenário 6): reage a falha de
 * rede com um estado próprio + retry, em vez de deixar a tela em branco.
 * Agora os horários são buscados junto da configuração (issue #139) para a
 * decisão de estado levar em conta os dois antes de renderizar.
 */
function useEstadoAlocacao(professorId: string): EstadoAlocacao {
  const [resultado, setResultado] = useState<CarregarEstadoResultado | undefined>(undefined);
  const [tentativa, setTentativa] = useState(0);
  const tentarNovamente = () => {
    setResultado(undefined);
    setTentativa((atual) => atual + 1);
  };
  useEffect(() => {
    let cancelado = false;
    carregarEstadoAlocacao(professorId).then((res) => {
      if (!cancelado) setResultado(res);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, tentativa]);
  return paraEstadoAlocacao(resultado, tentarNovamente);
}

function paraEstadoAlocacao(
  resultado: CarregarEstadoResultado | undefined,
  tentarNovamente: () => void,
): EstadoAlocacao {
  if (resultado === undefined) {
    return { tipo: 'carregando' };
  }
  if (!resultado.sucesso) {
    return { tipo: 'falha', mensagem: resultado.mensagem, tentarNovamente };
  }
  return resultado.estado;
}

async function carregarEstadoAlocacao(professorId: string): Promise<CarregarEstadoResultado> {
  const [resultadoConfiguracao, resultadoHorarios] = await Promise.all([
    obterConfiguracao(professorId),
    listarHorarios(professorId),
  ]);
  if (!resultadoConfiguracao.sucesso) {
    return { sucesso: false, mensagem: resultadoConfiguracao.mensagem };
  }
  if (!resultadoHorarios.sucesso) {
    return { sucesso: false, mensagem: resultadoHorarios.mensagem };
  }
  const carregamento: Extract<EstadoCarregamento, { status: 'carregada' }> =
    resultadoConfiguracao.definida
      ? {
          status: 'carregada',
          definida: true,
          modeloAgendamento: resultadoConfiguracao.modeloAgendamento,
        }
      : { status: 'carregada', definida: false };

  const totalAlocacoes =
    carregamento.definida && carregamento.modeloAgendamento === ModeloAgendamento.Vago
      ? await calcularTotalAlocacoes(resultadoHorarios.horarios)
      : 0;
  return {
    sucesso: true,
    estado: resolverEstadoAlocacao(carregamento, resultadoHorarios.horarios, totalAlocacoes),
  };
}

async function calcularTotalAlocacoes(horarios: Horario[]): Promise<number> {
  const alocacoesPorHorario = await carregarAlocacoes(horarios);
  return Object.values(alocacoesPorHorario).reduce((total, alocacoes) => total + alocacoes.length, 0);
}

/**
 * Mensagem por estado (issue #139). Rótulos exatos definidos em
 * docs/specs/139-mensagem-alocacao-clara/implementation.md — não usar o
 * nome interno do enum ("Vago") em texto visível; o rótulo público do
 * modelo é "Livre" (mesmo termo de `HorarioForm`/`HorarioCard`).
 */
function MensagemDoEstado({
  estado,
  professorId,
}: {
  estado: Extract<EstadoAlocacao, { tipo: 'sem-horarios' | 'modelo-nao-configurado' | 'modelo-livre' }>;
  professorId: string;
}) {
  switch (estado.tipo) {
    case 'sem-horarios':
      return (
        <View className="items-center gap-three">
          <ErrorMessage>Nenhum horário cadastrado ainda.</ErrorMessage>
          <Link href={`/professor/${professorId}/horarios`} asChild>
            <Button label="Cadastrar horários" variante="secundario" />
          </Link>
        </View>
      );
    case 'modelo-nao-configurado':
      return <ErrorMessage>Configure o modelo de agendamento antes de alocar Alunos.</ErrorMessage>;
    case 'modelo-livre':
      return (
        <ErrorMessage>
          {estado.temAlunosInscritos
            ? 'No modelo Livre, os Alunos se inscrevem sozinhos — não há atribuição manual pelo Professor.'
            : 'No modelo Livre, os Alunos se inscrevem sozinhos. Ainda ninguém se inscreveu em nenhum horário.'}
        </ErrorMessage>
      );
  }
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
        setErro(dados.erro);
      }
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  const handleAlocar = criarHandleAlocar(setAlocacoesPorHorario, setErro);
  const handleDesalocar = criarHandleDesalocar(setAlocacoesPorHorario, setErro);

  return { horarios, alunos, alocacoesPorHorario, erro, handleAlocar, handleDesalocar };
}

/**
 * Falha em `listarHorarios`/`listarAlunosProvisorios` antes era descartada
 * silenciosamente (`resultado.sucesso ? ... : []`), deixando a tela em
 * branco sem mensagem nem retry — achado de UX do qa-review no PR #30.
 * Agora a primeira falha encontrada vira `erro` e é exibida via
 * `<ErrorMessage>`, mesmo padrão já usado no gate de configuração acima.
 */
async function carregarTudo(professorId: string) {
  const [resultadoHorarios, resultadoAlunos] = await Promise.all([
    listarHorarios(professorId),
    listarAlunosProvisorios(),
  ]);
  const horarios = resultadoHorarios.sucesso ? resultadoHorarios.horarios : [];
  const alunos = resultadoAlunos.sucesso ? resultadoAlunos.alunos : [];
  const alocacoesPorHorario = await carregarAlocacoes(horarios);
  const erro = !resultadoHorarios.sucesso
    ? resultadoHorarios.mensagem
    : !resultadoAlunos.sucesso
      ? resultadoAlunos.mensagem
      : undefined;
  return { horarios, alunos, alocacoesPorHorario, erro };
}

async function carregarAlocacoes(horarios: Horario[]): Promise<AlocacoesPorHorario> {
  const resultados = await Promise.all(horarios.map((horario) => listarAlocacoes(horario.id)));
  const entradas = horarios.map((horario, indice) => {
    const resultado = resultados[indice];
    return [horario.id, resultado.sucesso ? resultado.alocacoes : []] as const;
  });
  return Object.fromEntries(entradas);
}

function criarHandleAlocar(
  setAlocacoesPorHorario: (atualizador: (atual: AlocacoesPorHorario) => AlocacoesPorHorario) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async (horarioId: string, matriculaId: string) => {
    setErro(undefined);
    const resultado = await alocarAluno(horarioId, matriculaId);
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
  setAlocacoesPorHorario: (atualizador: (atual: AlocacoesPorHorario) => AlocacoesPorHorario) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async (horarioId: string, matriculaId: string) => {
    setErro(undefined);
    const resultado = await desalocarAluno(horarioId, matriculaId);
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
