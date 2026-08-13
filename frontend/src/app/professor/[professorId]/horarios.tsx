import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { HorarioCard } from '@/components/organisms/HorarioCard';
import { HorarioForm } from '@/components/organisms/HorarioForm';
import { ModeloAgendamentoForm } from '@/components/organisms/ModeloAgendamentoForm';
import {
  definirModeloAgendamento,
  obterConfiguracao,
  type ModeloAgendamento,
} from '@/lib/api/configuracao';
import {
  criarHorario,
  listarHorarios,
  removerHorario,
  type CriarHorarioInput,
  type Horario,
} from '@/lib/api/horarios';

/**
 * Tela de horários disponíveis do Professor (issue #6), com gate de modelo
 * de agendamento na frente (issue #7): antes de mostrar a lista/form de
 * horários, confere se o Professor já definiu um modelo (`GET
 * configuracao`) — 404 mostra `ModeloAgendamentoForm` no lugar; ao definir,
 * a tela libera sem recarregar (só atualiza estado local) — ver
 * docs/specs/7-modelo-agendamento/implementation.md#edge-points.
 * `professorId` vem da rota em vez de uma sessão logada — não há login
 * ainda (issue #18, em paralelo).
 */
export default function HorariosProfessorScreen() {
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
        {carregamento.definida ? (
          <HorariosConteudo professorId={professorId} />
        ) : (
          <GateModeloAgendamento
            professorId={professorId}
            onDefinido={carregamento.marcarDefinida}
          />
        )}
      </View>
    </SafeAreaView>
  );
}

type ResultadoCarregamento =
  { sucesso: true; definida: boolean } | { sucesso: false; mensagem: string };

type EstadoCarregamento =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string; tentarNovamente: () => void }
  | { status: 'carregada'; definida: boolean; marcarDefinida: () => void };

/**
 * Consulta `GET configuracao` ao montar (issue #7) e expõe um jeito de
 * tentar de novo em caso de falha — sem isso, a tela ficava em branco pra
 * sempre numa falha de rede ou erro do servidor: o `useEffect` só atualizava
 * o estado no caminho de sucesso, então uma resposta `sucesso: false` nunca
 * saía do estado inicial (achado do dev-review/qa-review no PR #26,
 * regressão do guard rail que `fetchComTimeout` foi criado pra evitar — ver
 * issue #1 Cenário 6). O reset de `tentarNovamente` roda no clique (fora do
 * `useEffect`, que só deve reagir a dados externos, não disparar setState
 * síncrono no próprio corpo — `react-hooks/set-state-in-effect`).
 */
function useCarregamentoConfiguracao(professorId: string): EstadoCarregamento {
  const [resultado, setResultado] = useState<ResultadoCarregamento | undefined>(undefined);
  const [tentativa, setTentativa] = useState(0);
  const marcarDefinida = () => setResultado({ sucesso: true, definida: true });
  const tentarNovamente = () => {
    setResultado(undefined);
    setTentativa((atual) => atual + 1);
  };
  useEffect(() => {
    let cancelado = false;
    obterConfiguracao(professorId).then((res) => {
      if (!cancelado) setResultado(res.sucesso ? { sucesso: true, definida: res.definida } : res);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, tentativa]);
  return paraEstadoCarregamento(resultado, tentarNovamente, marcarDefinida);
}

/**
 * Deriva o estado de renderização a partir do resultado bruto da última
 * chamada — extraído de `useCarregamentoConfiguracao` pra manter o hook
 * dentro do orçamento de linhas do code-style (achado do dev-review,
 * rodada 2 do PR #26).
 */
function paraEstadoCarregamento(
  resultado: ResultadoCarregamento | undefined,
  tentarNovamente: () => void,
  marcarDefinida: () => void,
): EstadoCarregamento {
  if (resultado === undefined) {
    return { status: 'carregando' };
  }
  if (!resultado.sucesso) {
    return { status: 'falha', mensagem: resultado.mensagem, tentarNovamente };
  }
  return { status: 'carregada', definida: resultado.definida, marcarDefinida };
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

function GateModeloAgendamento({
  professorId,
  onDefinido,
}: {
  professorId: string;
  onDefinido: () => void;
}) {
  const [definindo, setDefinindo] = useState(false);
  const [erro, setErro] = useState<string | undefined>(undefined);

  async function handleSubmit(modelo: ModeloAgendamento) {
    setDefinindo(true);
    setErro(undefined);

    const resultado = await definirModeloAgendamento(professorId, modelo);

    setDefinindo(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    onDefinido();
  }

  return (
    <>
      <Heading level={1}>Modelo de agendamento</Heading>
      <ModeloAgendamentoForm enviando={definindo} erro={erro} onSubmit={handleSubmit} />
    </>
  );
}

function HorariosConteudo({ professorId }: { professorId: string }) {
  const [horarios, setHorarios] = useState<Horario[]>([]);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);

  useEffect(() => {
    let cancelado = false;

    listarHorarios(professorId).then((resultado) => {
      if (!cancelado && resultado.sucesso) {
        setHorarios(resultado.horarios);
      }
    });

    return () => {
      cancelado = true;
    };
  }, [professorId]);

  async function handleSubmit(input: CriarHorarioInput) {
    setEnviando(true);
    setErro(undefined);

    const resultado = await criarHorario(professorId, input);

    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) => [...atual, resultado.horario]);
  }

  async function handleRemover(horarioId: string) {
    setErro(undefined);

    const resultado = await removerHorario(professorId, horarioId);

    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) => atual.filter((horario) => horario.id !== horarioId));
  }

  return (
    <>
      <Heading level={1}>Horários disponíveis</Heading>
      <HorarioForm
        horariosExistentes={horarios}
        enviando={enviando}
        erro={erro}
        onSubmit={handleSubmit}
      />
      <FlatList
        data={horarios}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => <HorarioCard horario={item} onRemover={handleRemover} />}
        contentContainerClassName="gap-two"
      />
    </>
  );
}
