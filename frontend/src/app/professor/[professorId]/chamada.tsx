import { Link, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Pressable, SectionList, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { agruparPorDiaSemana } from '@/lib/agruparHorarios';
import { listarHorarios, type Horario } from '@/lib/api/horarios';
import { AlvoDeToqueMinimo, MaxContentWidth } from '@/theme/tokens';

const MensagemNenhumHorario = 'Nenhum horário cadastrado ainda.';

/**
 * Tela "Fazer chamada" do Professor: atalho direto pelo menu lateral pra
 * registrar presença, sem passar por "Gerenciar horários" (que é sobre
 * cadastrar/editar a política do horário, não sobre o dia a dia de dar
 * aula — modelos mentais diferentes, ver Nielsen #2/reconhecimento).
 * Reaproveita `listarHorarios` e o agrupamento por dia já usado em
 * `horarios.tsx`; cada item linka direto pra
 * `/professor/{professorId}/horarios/{horarioId}/chamada` (tela que já
 * existe, default pra data de hoje).
 */
export default function ChamadaProfessorScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const horarios = useCarregamentoHorarios(professorId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Fazer chamada" />
      <View
        className="w-full flex-1 self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        <ConteudoHorarios professorId={professorId} horarios={horarios} />
      </View>
    </SafeAreaView>
  );
}

function ConteudoHorarios({ professorId, horarios }: { professorId: string; horarios: Horario[] }) {
  if (horarios.length === 0) {
    return (
      <Text className="text-text-secondary dark:text-dark-text-secondary">
        {MensagemNenhumHorario}
      </Text>
    );
  }

  return (
    <SectionList
      sections={agruparPorDiaSemana(horarios)}
      keyExtractor={(item) => item.id}
      renderSectionHeader={({ section }) => <TituloDeSecao titulo={section.title} />}
      renderItem={({ item }) => <ItemDeHorario professorId={professorId} horario={item} />}
      contentContainerClassName="gap-two"
    />
  );
}

function TituloDeSecao({ titulo }: { titulo: string }) {
  return (
    <Text className="pb-one pt-three text-sm font-semibold text-text-secondary dark:text-dark-text-secondary">
      {titulo}
    </Text>
  );
}

function ItemDeHorario({ professorId, horario }: { professorId: string; horario: Horario }) {
  const horaFormatada = horario.horaInicio.slice(0, 5);

  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-base font-semibold text-text dark:text-dark-text">{horaFormatada}</Text>
      <Link href={`/professor/${professorId}/horarios/${horario.id}/chamada`} asChild>
        <Pressable
          accessibilityRole="button"
          className="items-center justify-center"
          style={AlvoDeToqueMinimo}
        >
          <Text className="text-sm font-semibold text-primary dark:text-dark-primary">
            Fazer chamada
          </Text>
        </Pressable>
      </Link>
    </View>
  );
}

function useCarregamentoHorarios(professorId: string): Horario[] {
  const [horarios, setHorarios] = useState<Horario[]>([]);

  useEffect(() => {
    let cancelado = false;
    listarHorarios(professorId).then((resultado) => {
      if (!cancelado && resultado.sucesso) setHorarios(resultado.horarios);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  return horarios;
}
