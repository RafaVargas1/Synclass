import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState, type Dispatch, type SetStateAction } from 'react';
import { FlatList, Pressable, ScrollView, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { HorarioCard } from '@/components/organisms/HorarioCard';
import { HorarioForm } from '@/components/organisms/HorarioForm';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import {
  alterarPrazoCancelamentoHorario,
  alterarTipoMarcacaoHorario,
  criarHorario,
  listarHorarios,
  removerHorario,
  type CriarHorarioInput,
  type Horario,
  type TipoMarcacao,
} from '@/lib/api/horarios';
import { NomesDiaSemana } from '@/lib/diaSemana';
import { useIsTelaLarga } from '@/lib/useIsTelaLarga';
import { AlvoDeToqueMinimo, MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de horários disponíveis do Professor (issue #6). Renderiza a
 * lista/formulário direto ao montar, sem gate anterior: cada horário
 * resolve sua própria política de marcação no cadastro (`HorarioForm`,
 * issue #76) — não existe mais "definir o modelo antes de tudo"
 * (`ModeloAgendamentoForm`, removido daqui na issue #76, ver
 * docs/specs/76-horario-form-politica/implementation.md). `professorId` vem
 * da rota em vez de uma sessão logada — não há login ainda (issue #18, em
 * paralelo).
 */
export default function HorariosProfessorScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Horários disponíveis" />
      <View
        className="w-full flex-1 gap-four self-center px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        <HorariosConteudo professorId={professorId} />
      </View>
    </SafeAreaView>
  );
}

const MensagemNenhumHorarioNoDia = 'Nenhum horário cadastrado para este dia.';

/**
 * Corpo da tela dividido em duas metades (issue de usabilidade reportada
 * pelo usuário): cadastrar um horário novo e ver os já cadastrados são duas
 * tarefas de peso igual, não uma principal e outra secundária — por isso
 * cada metade recebe `flex-1` (50/50), tanto lado a lado em viewport larga
 * quanto empilhada em viewport estreita (`flex-row` vs. coluna — em ambos
 * os casos as duas metades são irmãs `flex-1`, então dividem o espaço
 * disponível igualmente nos dois eixos, sem depender de altura fixa).
 *
 * Um único seletor de dia da semana (`AbasDeDiaSemana`) serve tanto pra
 * cadastrar quanto pra visualizar — o dia selecionado na aba é o mesmo dia
 * em que o novo horário é cadastrado (`HorarioForm` recebe `diaSemana` como
 * prop controlada, não tem mais o próprio seletor). Antes havia dois
 * seletores de dia (um em cada metade), o que confundia sobre qual dia
 * estava valendo pra cadastrar (achado de usabilidade do usuário). As abas
 * também destacam visualmente quais dias já têm horário cadastrado, sem
 * precisar rolar a semana inteira pra descobrir (reduz carga de opções
 * simultâneas, Lei de Hick).
 */
function HorariosConteudo({ professorId }: { professorId: string }) {
  const estado = useGerenciamentoHorarios(professorId);
  const telaLarga = useIsTelaLarga();
  const { diaSelecionado, selecionarDia, diasComHorario } = useDiaSelecionado(estado.horarios);
  const horariosDoDia = ordenarPorHorario(
    estado.horarios.filter((horario) => horario.diaSemana === diaSelecionado),
  );

  return (
    <View className={telaLarga ? 'w-full flex-1 flex-row gap-four' : 'w-full flex-1 gap-four'}>
      <ScrollView className="flex-1" contentContainerClassName="gap-four">
        <HorarioForm
          diaSemana={diaSelecionado}
          horariosExistentes={estado.horarios}
          enviando={estado.enviando}
          erro={estado.erro}
          onSubmit={estado.handleSubmit}
        />
      </ScrollView>
      <View className="flex-1 gap-two">
        <AbasDeDiaSemana
          diaSelecionado={diaSelecionado}
          diasComHorario={diasComHorario}
          onSelecionar={selecionarDia}
        />
        <ListaDoDia professorId={professorId} horarios={horariosDoDia} estado={estado} />
      </View>
    </View>
  );
}

function ListaDoDia({
  professorId,
  horarios,
  estado,
}: {
  professorId: string;
  horarios: Horario[];
  estado: ReturnType<typeof useGerenciamentoHorarios>;
}) {
  if (horarios.length === 0) {
    return (
      <Text className="text-text-secondary dark:text-dark-text-secondary">
        {MensagemNenhumHorarioNoDia}
      </Text>
    );
  }

  return (
    <FlatList
      data={horarios}
      keyExtractor={(item) => item.id}
      renderItem={({ item }) => (
        <HorarioCard
          professorId={professorId}
          horario={item}
          onRemover={estado.handleRemover}
          onAlterarPolitica={estado.handleAlterarPolitica}
          onAlterarPrazoCancelamento={estado.handleAlterarPrazoCancelamento}
        />
      )}
      contentContainerClassName="gap-two"
      style={{ flexGrow: 1 }}
    />
  );
}

function ordenarPorHorario(horarios: Horario[]): Horario[] {
  return [...horarios].sort((a, b) => a.horaInicio.localeCompare(b.horaInicio));
}

/** Seleciona o dia de hoje por padrão — é o dia que o Professor mais
 *  provavelmente veio conferir. */
function useDiaSelecionado(horarios: Horario[]) {
  const [diaSelecionado, setDiaSelecionado] = useState(() => new Date().getDay());
  const diasComHorario = new Set(horarios.map((horario) => horario.diaSemana));
  return { diaSelecionado, selecionarDia: setDiaSelecionado, diasComHorario };
}

function AbasDeDiaSemana({
  diaSelecionado,
  diasComHorario,
  onSelecionar,
}: {
  diaSelecionado: number;
  diasComHorario: Set<number>;
  onSelecionar: (dia: number) => void;
}) {
  return (
    <View testID="abas-dia-semana" accessibilityRole="tablist" className="flex-row flex-wrap gap-one">
      {NomesDiaSemana.map((nome, dia) => (
        <AbaDeDia
          key={dia}
          nome={nome}
          selecionado={dia === diaSelecionado}
          temHorario={diasComHorario.has(dia)}
          onPress={() => onSelecionar(dia)}
        />
      ))}
    </View>
  );
}

function AbaDeDia({
  nome,
  selecionado,
  temHorario,
  onPress,
}: {
  nome: string;
  selecionado: boolean;
  temHorario: boolean;
  onPress: () => void;
}) {
  const estilo = selecionado
    ? 'border-primary bg-primary dark:border-dark-primary dark:bg-dark-primary'
    : temHorario
      ? 'border-primary bg-background-element dark:border-dark-primary dark:bg-dark-background-element'
      : 'border-background-selected bg-background-element dark:border-dark-background-selected dark:bg-dark-background-element';
  const corDoTexto = selecionado ? 'text-background dark:text-dark-background' : 'text-text dark:text-dark-text';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      onPress={onPress}
      className={`items-center justify-center rounded-small border px-two py-one ${estilo}`}
      style={AlvoDeToqueMinimo}
    >
      <Text className={`text-sm font-medium ${corDoTexto}`}>{nome.slice(0, 3)}</Text>
    </Pressable>
  );
}

/**
 * Extraída de `HorariosConteudo` (issue #6, achado de tamanho de função do
 * dev-review na rodada 3 do PR #26 — o corpo já vinha grande antes deste
 * card, decompor era pendente). `handleSubmit`/`handleRemover` viram fábricas
 * de closure à parte (`criarHandleSubmit`/`criarHandleRemover`) em vez de
 * `function`s aninhadas, pra este hook também caber no orçamento de linhas.
 */
function useGerenciamentoHorarios(professorId: string) {
  const [horarios, setHorarios] = useState<Horario[]>([]);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [enviando, setEnviando] = useState(false);
  const handleSubmit = criarHandleSubmit(professorId, setHorarios, setErro, setEnviando);
  const handleRemover = criarHandleRemover(professorId, setHorarios, setErro);
  const handleAlterarPolitica = criarHandleAlterarPolitica(professorId, setHorarios, setErro);
  const handleAlterarPrazoCancelamento = criarHandleAlterarPrazoCancelamento(
    professorId,
    setHorarios,
    setErro,
  );

  useEffect(() => {
    let cancelado = false;
    listarHorarios(professorId).then((resultado) => {
      if (!cancelado && resultado.sucesso) setHorarios(resultado.horarios);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  return {
    horarios,
    erro,
    enviando,
    handleSubmit,
    handleRemover,
    handleAlterarPolitica,
    handleAlterarPrazoCancelamento,
  };
}

function criarHandleSubmit(
  professorId: string,
  setHorarios: Dispatch<SetStateAction<Horario[]>>,
  setErro: Dispatch<SetStateAction<string | undefined>>,
  setEnviando: Dispatch<SetStateAction<boolean>>,
) {
  return async (input: CriarHorarioInput) => {
    setEnviando(true);
    setErro(undefined);
    const resultado = await criarHorario(professorId, input);
    setEnviando(false);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) => [...atual, resultado.horario]);
  };
}

function criarHandleRemover(
  professorId: string,
  setHorarios: Dispatch<SetStateAction<Horario[]>>,
  setErro: Dispatch<SetStateAction<string | undefined>>,
) {
  return async (horarioId: string) => {
    setErro(undefined);
    const resultado = await removerHorario(professorId, horarioId);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) => atual.filter((horario) => horario.id !== horarioId));
  };
}

function criarHandleAlterarPolitica(
  professorId: string,
  setHorarios: Dispatch<SetStateAction<Horario[]>>,
  setErro: Dispatch<SetStateAction<string | undefined>>,
) {
  return async (horarioId: string, tipoMarcacao: TipoMarcacao) => {
    setErro(undefined);
    const resultado = await alterarTipoMarcacaoHorario(professorId, horarioId, tipoMarcacao);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) =>
      atual.map((horario) => (horario.id === horarioId ? resultado.horario : horario)),
    );
  };
}

function criarHandleAlterarPrazoCancelamento(
  professorId: string,
  setHorarios: Dispatch<SetStateAction<Horario[]>>,
  setErro: Dispatch<SetStateAction<string | undefined>>,
) {
  return async (horarioId: string, prazoCancelamentoMinutos: number) => {
    setErro(undefined);
    const resultado = await alterarPrazoCancelamentoHorario(
      professorId,
      horarioId,
      prazoCancelamentoMinutos,
    );
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) =>
      atual.map((horario) => (horario.id === horarioId ? resultado.horario : horario)),
    );
  };
}
