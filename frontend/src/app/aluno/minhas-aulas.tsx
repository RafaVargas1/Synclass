import { useEffect, useState } from 'react';
import { ActivityIndicator, Pressable, SectionList, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { AulaProximaCard } from '@/components/organisms/AulaProximaCard';
import { HorarioVagoCard } from '@/components/organisms/HorarioVagoCard';
import { ModalConfirmacao } from '@/components/organisms/ModalConfirmacao';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { agruparPorData, agruparPorDiaSemana } from '@/lib/agruparHorarios';
import { cancelarAula, listarProximasAulas, type AulaProxima } from '@/lib/api/cancelamentos';
import { confirmarPresenca } from '@/lib/api/frequencias';
import { listarHorariosVagos, marcarHorario, type HorarioVago } from '@/lib/api/marcacoes';
import { listarVinculosAluno, type VinculoProfessor } from '@/lib/api/vinculosAluno';
import { NomesDiaSemana } from '@/lib/diaSemana';
import { AlvoDeToqueMinimo, MaxContentWidth } from '@/theme/tokens';

const MensagemNenhumProfessor = 'Você ainda não está vinculado a nenhum Professor.';
const MensagemNenhumHorarioVago = 'Nenhum horário disponível para marcação no momento.';
const MensagemNenhumaAulaProxima = 'Você ainda não tem nenhuma aula marcada.';

/**
 * Tela única "Minhas aulas" do Aluno — substitui as duas telas por
 * Professor (`/aluno/professores/[professorId]/{horarios,minhas-aulas}`) e
 * a tela intermediária "Meus Professores": um Aluno com mais de um
 * Professor escolhe o foco por um seletor de abas nesta própria página, em
 * vez de navegar por uma lista de vínculos primeiro. Combina, pro
 * Professor selecionado, as aulas já marcadas (cancelar/confirmar
 * presença) e os horários vagos (marcar) — ambas agrupadas por dia, pra
 * reduzir carga visual de listas longas (Gestalt/Hick, ver
 * docs/spec/ux-heuristics.md).
 */
export default function MinhasAulasAlunoScreen() {
  const vinculos = useCarregamentoVinculos();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Minhas aulas" />
      <View
        className="w-full flex-1 self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        <ConteudoVinculos vinculos={vinculos} />
      </View>
    </SafeAreaView>
  );
}

function ConteudoVinculos({ vinculos }: { vinculos: EstadoVinculos }) {
  if (vinculos.status === 'carregando') {
    return <ActivityIndicator accessibilityLabel="Carregando" />;
  }
  if (vinculos.status === 'falha') {
    return <ErrorMessage>{vinculos.mensagem}</ErrorMessage>;
  }
  if (vinculos.professores.length === 0) {
    return <ErrorMessage>{MensagemNenhumProfessor}</ErrorMessage>;
  }
  return <ConteudoPorProfessor professores={vinculos.professores} />;
}

type EstadoVinculos =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string }
  | { status: 'carregado'; professores: VinculoProfessor[] };

function useCarregamentoVinculos(): EstadoVinculos {
  const [estado, setEstado] = useState<EstadoVinculos>({ status: 'carregando' });

  useEffect(() => {
    let cancelado = false;
    listarVinculosAluno().then((resultado) => {
      if (cancelado) return;
      setEstado(
        resultado.sucesso
          ? { status: 'carregado', professores: resultado.vinculos }
          : { status: 'falha', mensagem: resultado.mensagem },
      );
    });
    return () => {
      cancelado = true;
    };
  }, []);

  return estado;
}

function ConteudoPorProfessor({ professores }: { professores: VinculoProfessor[] }) {
  const [professorId, setProfessorId] = useState(professores[0].professorId);

  return (
    <>
      {professores.length > 1 ? (
        <SeletorDeProfessor
          professores={professores}
          professorId={professorId}
          onSelecionar={setProfessorId}
        />
      ) : null}
      <ConteudoDoProfessor key={professorId} professorId={professorId} />
    </>
  );
}

function SeletorDeProfessor({
  professores,
  professorId,
  onSelecionar,
}: {
  professores: VinculoProfessor[];
  professorId: string;
  onSelecionar: (professorId: string) => void;
}) {
  return (
    <View accessibilityRole="tablist" className="flex-row flex-wrap gap-one">
      {professores.map((professor) => (
        <AbaDeProfessor
          key={professor.professorId}
          professor={professor}
          selecionado={professor.professorId === professorId}
          onPress={() => onSelecionar(professor.professorId)}
        />
      ))}
    </View>
  );
}

function AbaDeProfessor({
  professor,
  selecionado,
  onPress,
}: {
  professor: VinculoProfessor;
  selecionado: boolean;
  onPress: () => void;
}) {
  const corDeFundo = selecionado
    ? 'border-primary bg-primary dark:border-dark-primary dark:bg-dark-primary'
    : 'border-background-selected bg-background-element dark:border-dark-background-selected dark:bg-dark-background-element';
  const corDoTexto = selecionado ? 'text-white' : 'text-text dark:text-dark-text';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      onPress={onPress}
      className={`items-center justify-center rounded-small border px-two py-one ${corDeFundo}`}
      style={AlvoDeToqueMinimo}
    >
      <Text className={corDoTexto}>{professor.nome}</Text>
    </Pressable>
  );
}

function ConteudoDoProfessor({ professorId }: { professorId: string }) {
  const aulas = useGerenciamentoAulas(professorId);
  const vagos = useGerenciamentoHorariosVagos(professorId);

  return (
    <>
      {aulas.erro ? <ErrorMessage>{aulas.erro}</ErrorMessage> : null}
      {vagos.erro ? <ErrorMessage>{vagos.erro}</ErrorMessage> : null}
      <SecaoMinhasAulas
        aulas={aulas.aulas}
        confirmadas={aulas.confirmadas}
        onCancelar={aulas.pedirCancelamento}
        onConfirmar={aulas.handleConfirmar}
      />
      <SecaoHorariosVagos horarios={vagos.horarios} onMarcar={vagos.pedirMarcacao} />
      <ModalDeCancelamento aulas={aulas} />
      <ModalDeMarcacao vagos={vagos} />
    </>
  );
}

function ModalDeCancelamento({ aulas }: { aulas: EstadoAulas }) {
  const aula = aulas.pendenteDeCancelar;
  return (
    <ModalConfirmacao
      visivel={aula !== null}
      titulo="Confirmar cancelamento"
      mensagem={aula ? mensagemDeHorario('Cancelar a aula de', aula) : ''}
      rotuloConfirmar="Cancelar aula"
      destrutivo
      onConfirmar={aulas.confirmarCancelamento}
      onFechar={aulas.desistirDeCancelar}
    />
  );
}

function ModalDeMarcacao({ vagos }: { vagos: EstadoHorariosVagos }) {
  const horario = vagos.pendenteDeMarcar;
  return (
    <ModalConfirmacao
      visivel={horario !== null}
      titulo="Confirmar marcação"
      mensagem={horario ? mensagemDeHorario('Marcar o horário de', horario) : ''}
      rotuloConfirmar="Marcar"
      onConfirmar={vagos.confirmarMarcacao}
      onFechar={vagos.desistirDeMarcar}
    />
  );
}

function mensagemDeHorario(prefixo: string, item: { diaSemana: number; horaInicio: string }): string {
  return `${prefixo} ${NomesDiaSemana[item.diaSemana]} às ${item.horaInicio.slice(0, 5)}?`;
}

function SecaoMinhasAulas({
  aulas,
  confirmadas,
  onCancelar,
  onConfirmar,
}: {
  aulas: AulaProxima[];
  confirmadas: Set<string>;
  onCancelar: (aula: AulaProxima) => void;
  onConfirmar: (horarioId: string, data: string) => void;
}) {
  return (
    <View className="gap-two">
      <Text className="text-lg font-semibold text-text dark:text-dark-text">Minhas aulas</Text>
      {aulas.length === 0 ? (
        <Text className="text-text-secondary dark:text-dark-text-secondary">
          {MensagemNenhumaAulaProxima}
        </Text>
      ) : (
        <SectionList
          sections={agruparPorData(aulas)}
          keyExtractor={(item) => chaveDaAula(item.horarioId, item.data)}
          renderSectionHeader={({ section }) => <TituloDeSecao titulo={section.title} />}
          renderItem={({ item }) => (
            <AulaProximaCard
              aulaProxima={item}
              confirmado={confirmadas.has(chaveDaAula(item.horarioId, item.data))}
              onCancelar={() => onCancelar(item)}
              onConfirmar={onConfirmar}
            />
          )}
          contentContainerClassName="gap-two"
          scrollEnabled={false}
        />
      )}
    </View>
  );
}

function SecaoHorariosVagos({
  horarios,
  onMarcar,
}: {
  horarios: HorarioVago[];
  onMarcar: (horario: HorarioVago) => void;
}) {
  return (
    <View className="gap-two">
      <Text className="text-lg font-semibold text-text dark:text-dark-text">Horários disponíveis</Text>
      {horarios.length === 0 ? (
        <Text className="text-text-secondary dark:text-dark-text-secondary">
          {MensagemNenhumHorarioVago}
        </Text>
      ) : (
        <SectionList
          sections={agruparPorDiaSemana(horarios)}
          keyExtractor={(item) => item.id}
          renderSectionHeader={({ section }) => <TituloDeSecao titulo={section.title} />}
          renderItem={({ item }) => <HorarioVagoCard horarioVago={item} onMarcar={() => onMarcar(item)} />}
          contentContainerClassName="gap-two"
          scrollEnabled={false}
        />
      )}
    </View>
  );
}

function TituloDeSecao({ titulo }: { titulo: string }) {
  return (
    <Text className="pb-one pt-three text-sm font-semibold text-text-secondary dark:text-dark-text-secondary">
      {titulo}
    </Text>
  );
}

/**
 * Identifica de forma única uma ocorrência (horário + data) dentro da
 * sessão da tela, para o estado local otimista de confirmação (issue #15)
 * — `horarioId` sozinho não basta, a mesma alocação recorrente reaparece
 * com datas diferentes a cada semana.
 */
function chaveDaAula(horarioId: string, data: string): string {
  return `${horarioId}|${data}`;
}

type EstadoAulas = {
  aulas: AulaProxima[];
  erro: string | undefined;
  confirmadas: Set<string>;
  pendenteDeCancelar: AulaProxima | null;
  pedirCancelamento: (aula: AulaProxima) => void;
  desistirDeCancelar: () => void;
  confirmarCancelamento: () => void;
  handleConfirmar: (horarioId: string, data: string) => void;
};

function useGerenciamentoAulas(professorId: string): EstadoAulas {
  const [aulas, setAulas] = useState<AulaProxima[]>([]);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [confirmadas, setConfirmadas] = useState<Set<string>>(new Set());
  const [pendenteDeCancelar, setPendenteDeCancelar] = useState<AulaProxima | null>(null);

  useEffect(() => {
    let cancelado = false;
    carregarProximasAulas(professorId, setAulas, setErro, () => cancelado);
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  return {
    aulas,
    erro,
    confirmadas,
    pendenteDeCancelar,
    pedirCancelamento: setPendenteDeCancelar,
    desistirDeCancelar: () => setPendenteDeCancelar(null),
    confirmarCancelamento: criarConfirmarCancelamento(
      professorId,
      pendenteDeCancelar,
      setPendenteDeCancelar,
      setAulas,
      setErro,
    ),
    handleConfirmar: criarHandleConfirmar(professorId, setConfirmadas, setErro),
  };
}

async function carregarProximasAulas(
  professorId: string,
  setAulas: (aulas: AulaProxima[]) => void,
  setErro: (mensagem: string | undefined) => void,
  foiCancelado: () => boolean,
) {
  const resultado = await listarProximasAulas(professorId);
  if (foiCancelado()) return;
  if (!resultado.sucesso) {
    setErro(resultado.mensagem);
    return;
  }
  setAulas(resultado.aulas);
}

function criarConfirmarCancelamento(
  professorId: string,
  pendente: AulaProxima | null,
  setPendente: (aula: AulaProxima | null) => void,
  setAulas: (aulas: AulaProxima[]) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async () => {
    if (!pendente) return;
    setErro(undefined);
    const resultado = await cancelarAula(professorId, pendente.horarioId, pendente.data);
    setPendente(null);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    // Recarrega em vez de remover só pelo horarioId: a alocação recorrente
    // continua (AC3), então a próxima ocorrência daquele horário deve
    // reaparecer na lista, não sumir permanentemente.
    await carregarProximasAulas(professorId, setAulas, setErro, () => false);
  };
}

function criarHandleConfirmar(
  professorId: string,
  setConfirmadas: (atualizar: (confirmadas: Set<string>) => Set<string>) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async (horarioId: string, data: string) => {
    setErro(undefined);
    const resultado = await confirmarPresenca(professorId, horarioId, data);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setConfirmadas((atual) => new Set(atual).add(chaveDaAula(horarioId, data)));
  };
}

type EstadoHorariosVagos = {
  horarios: HorarioVago[];
  erro: string | undefined;
  pendenteDeMarcar: HorarioVago | null;
  pedirMarcacao: (horario: HorarioVago) => void;
  desistirDeMarcar: () => void;
  confirmarMarcacao: () => void;
};

function useGerenciamentoHorariosVagos(professorId: string): EstadoHorariosVagos {
  const [horarios, setHorarios] = useState<HorarioVago[]>([]);
  const [erro, setErro] = useState<string | undefined>(undefined);
  const [pendenteDeMarcar, setPendenteDeMarcar] = useState<HorarioVago | null>(null);

  useEffect(() => {
    let cancelado = false;
    listarHorariosVagos(professorId).then((resultado) => {
      if (cancelado) return;
      if (!resultado.sucesso) {
        setErro(resultado.mensagem);
        return;
      }
      setHorarios(resultado.horarios);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  return {
    horarios,
    erro,
    pendenteDeMarcar,
    pedirMarcacao: setPendenteDeMarcar,
    desistirDeMarcar: () => setPendenteDeMarcar(null),
    confirmarMarcacao: criarConfirmarMarcacao(professorId, pendenteDeMarcar, setPendenteDeMarcar, setHorarios, setErro),
  };
}

function criarConfirmarMarcacao(
  professorId: string,
  pendente: HorarioVago | null,
  setPendente: (horario: HorarioVago | null) => void,
  setHorarios: (atualizador: (atual: HorarioVago[]) => HorarioVago[]) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async () => {
    if (!pendente) return;
    setErro(undefined);
    const resultado = await marcarHorario(professorId, pendente.id);
    setPendente(null);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) => atual.filter((horario) => horario.id !== pendente.id));
  };
}
