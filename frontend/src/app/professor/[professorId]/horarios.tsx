import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

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
  const configuracao = useConfiguracaoProfessor(professorId);

  if (!configuracao.carregada) {
    return null;
  }

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        {configuracao.definida ? (
          <HorariosConteudo professorId={professorId} />
        ) : (
          <>
            <Heading level={1}>Modelo de agendamento</Heading>
            <ModeloAgendamentoForm
              enviando={configuracao.definindo}
              erro={configuracao.erro}
              onSubmit={configuracao.definirModelo}
            />
          </>
        )}
      </View>
    </SafeAreaView>
  );
}

type EstadoConfiguracao =
  | { carregada: false }
  | {
      carregada: true;
      definida: boolean;
      definindo: boolean;
      erro?: string;
      definirModelo: (modelo: ModeloAgendamento) => void;
    };

/**
 * Encapsula o gate de configuração (issue #7): consulta `GET configuracao`
 * ao montar e expõe `definirModelo`, que libera a tela normal sem precisar
 * recarregar a página — só atualiza o estado local (`definida: true`) em vez
 * de navegar ou refazer o fetch inicial.
 */
function useConfiguracaoProfessor(professorId: string): EstadoConfiguracao {
  const [definida, setDefinida] = useState<boolean | undefined>(undefined);
  const [definindo, setDefinindo] = useState(false);
  const [erro, setErro] = useState<string | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;

    obterConfiguracao(professorId).then((resultado) => {
      if (!cancelado && resultado.sucesso) {
        setDefinida(resultado.definida);
      }
    });

    return () => {
      cancelado = true;
    };
  }, [professorId]);

  async function definirModelo(modelo: ModeloAgendamento) {
    setDefinindo(true);
    setErro(undefined);

    const resultado = await definirModeloAgendamento(professorId, modelo);

    setDefinindo(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setDefinida(true);
  }

  if (definida === undefined) {
    return { carregada: false };
  }
  return { carregada: true, definida, definindo, erro, definirModelo };
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
