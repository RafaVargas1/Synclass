import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { FlatList, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { HorarioVagoCard } from '@/components/organisms/HorarioVagoCard';
import { listarHorariosVagos, marcarHorario, type HorarioVago } from '@/lib/api/marcacoes';

const MensagemNenhumHorarioVago = 'Nenhum horário disponível para marcação no momento.';

/**
 * Tela do Aluno para marcar livremente um horário vago com um Professor
 * (issue #9) — sem gate de modelo de agendamento na frente (diferente de
 * `alocacoes.tsx`, issue #8): o próprio `GET vagos` já devolve lista vazia
 * quando o modelo não permite nada, então a tela mostra um estado vazio em
 * vez de bloquear. Rota de dois segmentos (`matriculaId`/`professorId`
 * explícitos), mesma decisão de `alocacoes.tsx` — sem sessão real ainda
 * (débito técnico #23, ver docs/specs/9-aluno-marca-horario-vago/implementation.md).
 */
export default function HorariosVagosAlunoScreen() {
  const { professorId, matriculaId } = useLocalSearchParams<{
    professorId: string;
    matriculaId: string;
  }>();
  const estado = useGerenciamentoHorariosVagos(professorId, matriculaId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        <Heading level={1} accessibilityRole="header">Horários disponíveis</Heading>
        {estado.erro ? <ErrorMessage>{estado.erro}</ErrorMessage> : null}
        <ConteudoHorariosVagos horarios={estado.horarios} onMarcar={estado.handleMarcar} />
      </View>
    </SafeAreaView>
  );
}

function ConteudoHorariosVagos({
  horarios,
  onMarcar,
}: {
  horarios: HorarioVago[];
  onMarcar: (horarioId: string) => void;
}) {
  if (horarios.length === 0) {
    return (
      <Text className="text-text-secondary dark:text-dark-text-secondary">
        {MensagemNenhumHorarioVago}
      </Text>
    );
  }

  return (
    <FlatList
      data={horarios}
      keyExtractor={(item) => item.id}
      renderItem={({ item }) => <HorarioVagoCard horarioVago={item} onMarcar={onMarcar} />}
      contentContainerClassName="gap-two"
    />
  );
}

/**
 * Carrega os horários vagos ao montar e expõe o handler de marcar — mesmo
 * padrão de `horarios.tsx#useGerenciamentoHorarios` (issue #6/#7).
 */
function useGerenciamentoHorariosVagos(professorId: string, matriculaId: string) {
  const [horarios, setHorarios] = useState<HorarioVago[]>([]);
  const [erro, setErro] = useState<string | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    listarHorariosVagos(professorId, matriculaId).then((resultado) => {
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
  }, [professorId, matriculaId]);

  const handleMarcar = criarHandleMarcar(professorId, matriculaId, setHorarios, setErro);
  return { horarios, erro, handleMarcar };
}

function criarHandleMarcar(
  professorId: string,
  matriculaId: string,
  setHorarios: (atualizador: (atual: HorarioVago[]) => HorarioVago[]) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async (horarioId: string) => {
    setErro(undefined);
    const resultado = await marcarHorario(professorId, horarioId, matriculaId);
    if (!resultado.sucesso) {
      setErro(resultado.mensagem);
      return;
    }
    setHorarios((atual) => atual.filter((horario) => horario.id !== horarioId));
  };
}
