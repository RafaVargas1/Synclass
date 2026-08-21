import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState, type Dispatch, type SetStateAction } from 'react';
import { FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { HorarioCard } from '@/components/organisms/HorarioCard';
import { HorarioForm } from '@/components/organisms/HorarioForm';
import { Topbar } from '@/components/organisms/Topbar';
import {
  alterarTipoMarcacaoHorario,
  criarHorario,
  listarHorarios,
  removerHorario,
  type CriarHorarioInput,
  type Horario,
  type TipoMarcacao,
} from '@/lib/api/horarios';
import { MaxContentWidth } from '@/theme/tokens';

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
      <Topbar titulo="Horários disponíveis" />
      <View
        className="w-full flex-1 self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        <HorariosConteudo professorId={professorId} />
      </View>
    </SafeAreaView>
  );
}

function HorariosConteudo({ professorId }: { professorId: string }) {
  const estado = useGerenciamentoHorarios(professorId);
  return (
    <>
      <HorarioForm
        horariosExistentes={estado.horarios}
        enviando={estado.enviando}
        erro={estado.erro}
        onSubmit={estado.handleSubmit}
      />
      <FlatList
        data={estado.horarios}
        keyExtractor={(item) => item.id}
        renderItem={({ item }) => (
          <HorarioCard
            horario={item}
            onRemover={estado.handleRemover}
            onAlterarPolitica={estado.handleAlterarPolitica}
          />
        )}
        contentContainerClassName="gap-two"
      />
    </>
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

  useEffect(() => {
    let cancelado = false;
    listarHorarios(professorId).then((resultado) => {
      if (!cancelado && resultado.sucesso) setHorarios(resultado.horarios);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  return { horarios, erro, enviando, handleSubmit, handleRemover, handleAlterarPolitica };
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
