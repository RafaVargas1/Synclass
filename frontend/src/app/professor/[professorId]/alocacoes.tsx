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
import { listarHorarios, TipoMarcacao, type Horario } from '@/lib/api/horarios';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de alocação de Aluno em horário (issue #8). O gate de estado
 * (`resolverEstadoAlocacao`, issue #139/#157) decide por HORÁRIO, não mais
 * por Professor: cada horário já carrega seu próprio `tipoMarcacao`
 * (issue #76) — não existe mais um "modelo de agendamento" único por
 * Professor (`ConfiguracaoProfessor`/`ModeloAgendamentoForm` foram
 * removidos de `horarios.tsx` na própria issue #76 e nunca substituídos
 * por outra tela; a issue #157 corrigiu esta tela, que ainda checava esse
 * conceito morto e por isso nunca saía do estado "configure o modelo").
 * `professorId` continua vindo da rota (issue #23 não muda `horarios.tsx`/
 * `HorariosController`, chamado aqui também — fora de escopo).
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
          <AlocacoesConteudo professorId={professorId} horariosAlocaveis={estado.horariosAlocaveis} />
        ) : (
          <MensagemDoEstado estado={estado} professorId={professorId} />
        )}
      </View>
    </SafeAreaView>
  );
}

type EstadoAlocacaoResolvido =
  | { tipo: 'sem-horarios' }
  | { tipo: 'todos-livres' }
  | { tipo: 'permite-alocacao'; horariosAlocaveis: Horario[] };

type EstadoAlocacao =
  | { tipo: 'carregando' }
  | { tipo: 'falha'; mensagem: string; tentarNovamente: () => void }
  | EstadoAlocacaoResolvido;

/**
 * Decisão de estado da tela, POR HORÁRIO (issue #157 — cada horário já
 * carrega seu próprio `tipoMarcacao`, não existe mais um modelo único por
 * Professor). Horários Livre não entram na grade de alocação (o Aluno se
 * inscreve sozinho nesse tipo); Fixo/Híbrido entram.
 */
function resolverEstadoAlocacao(horarios: Horario[]): EstadoAlocacaoResolvido {
  if (horarios.length === 0) {
    return { tipo: 'sem-horarios' };
  }
  const horariosAlocaveis = horarios.filter((horario) => horario.tipoMarcacao !== TipoMarcacao.Livre);
  if (horariosAlocaveis.length === 0) {
    return { tipo: 'todos-livres' };
  }
  return { tipo: 'permite-alocacao', horariosAlocaveis };
}

type CarregarEstadoResultado =
  | { sucesso: true; estado: EstadoAlocacaoResolvido }
  | { sucesso: false; mensagem: string };

/**
 * Mesma estratégia de `horarios.tsx#useCarregamentoConfiguracao` (achado do
 * dev-review/qa-review no PR #26, ver issue #1 Cenário 6): reage a falha de
 * rede com um estado próprio + retry, em vez de deixar a tela em branco.
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
  const resultadoHorarios = await listarHorarios(professorId);
  if (!resultadoHorarios.sucesso) {
    return { sucesso: false, mensagem: resultadoHorarios.mensagem };
  }
  return { sucesso: true, estado: resolverEstadoAlocacao(resultadoHorarios.horarios) };
}

/**
 * Mensagem por estado (issue #139/#157). Não usar o nome interno do enum
 * ("Vago") em texto visível — o rótulo público do tipo é "Livre" (mesmo
 * termo de `HorarioForm`/`HorarioCard`).
 */
function MensagemDoEstado({
  estado,
  professorId,
}: {
  estado: Extract<EstadoAlocacao, { tipo: 'sem-horarios' | 'todos-livres' }>;
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
    case 'todos-livres':
      return (
        <ErrorMessage>
          Nenhum dos seus horários aceita atribuição manual — todos são do tipo Livre, os Alunos se
          inscrevem sozinhos.
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

function AlocacoesConteudo({
  professorId,
  horariosAlocaveis,
}: {
  professorId: string;
  horariosAlocaveis: Horario[];
}) {
  const estado = useGerenciamentoAlocacoes(professorId, horariosAlocaveis);
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
 * Alunos e alocações dos horários já filtrados (Fixo/Híbrido, resolvidos
 * por `resolverEstadoAlocacao` antes deste componente montar) — extraído
 * de `AlocacoesConteudo` no mesmo padrão de
 * `horarios.tsx#useGerenciamentoHorarios` (achado de tamanho de função do
 * dev-review, PR #26). Não busca horários de novo (já vieram prontos do
 * gate de estado, issue #157) — só Alunos e alocações.
 */
function useGerenciamentoAlocacoes(professorId: string, horarios: Horario[]) {
  const [alunos, setAlunos] = useState<AlunoProvisorio[]>([]);
  const [alocacoesPorHorario, setAlocacoesPorHorario] = useState<AlocacoesPorHorario>({});
  const [erro, setErro] = useState<string | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    carregarAlunosEAlocacoes(horarios).then((dados) => {
      if (!cancelado) {
        setAlunos(dados.alunos);
        setAlocacoesPorHorario(dados.alocacoesPorHorario);
        setErro(dados.erro);
      }
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, horarios]);

  const handleAlocar = criarHandleAlocar(setAlocacoesPorHorario, setErro);
  const handleDesalocar = criarHandleDesalocar(setAlocacoesPorHorario, setErro);

  return { horarios, alunos, alocacoesPorHorario, erro, handleAlocar, handleDesalocar };
}

/**
 * Falha em `listarAlunosProvisorios` antes era descartada silenciosamente
 * (`resultado.sucesso ? ... : []`), deixando a tela em branco sem mensagem
 * nem retry — achado de UX do qa-review no PR #30. A primeira falha
 * encontrada vira `erro` e é exibida via `<ErrorMessage>`.
 */
async function carregarAlunosEAlocacoes(horarios: Horario[]) {
  const [resultadoAlunos, alocacoesPorHorario] = await Promise.all([
    listarAlunosProvisorios(),
    carregarAlocacoes(horarios),
  ]);
  const alunos = resultadoAlunos.sucesso ? resultadoAlunos.alunos : [];
  const erro = !resultadoAlunos.sucesso ? resultadoAlunos.mensagem : undefined;
  return { alunos, alocacoesPorHorario, erro };
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
