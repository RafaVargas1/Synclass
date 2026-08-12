import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Heading } from '@/components/atoms/Heading';
import { HorarioCard } from '@/components/organisms/HorarioCard';
import { HorarioForm } from '@/components/organisms/HorarioForm';
import {
  criarHorario,
  listarHorarios,
  removerHorario,
  type CriarHorarioInput,
  type Horario,
} from '@/lib/api/horarios';

/**
 * Tela de horários disponíveis do Professor (issue #6). `professorId` vem
 * da rota (`professor/[professorId]/horarios`) em vez de uma sessão logada
 * — não há login ainda (issue #18, em paralelo); ver decisão documentada em
 * docs/specs/6-horarios-disponiveis/implementation.md#edge-points.
 */
export default function HorariosProfessorScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
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
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
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
      </View>
    </SafeAreaView>
  );
}
