import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { ProfessorVinculadoCard } from '@/components/organisms/ProfessorVinculadoCard';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { listarVinculosAluno, type VinculoProfessor } from '@/lib/api/vinculosAluno';
import { MaxContentWidth } from '@/theme/tokens';

const MensagemNenhumProfessor = 'Você ainda não está vinculado a nenhum Professor.';

/**
 * Tela "Meus Professores" (issue #182) — ponto de entrada que faltava para
 * o Aluno alcançar as telas por Professor (`/aluno/professores/[professorId]/
 * minhas-aulas`, `.../horarios`): antes desta tela, essas rotas existiam e
 * funcionavam mas eram inalcançáveis por navegação (só digitando a URL com
 * o `professorId` exato, que o Aluno não tem como saber) — ver
 * docs/spec/decisions nota de limitação conhecida em `secoesPorPapel.ts`
 * (o Aluno não tinha como saber os `professorId` dos próprios vínculos).
 */
export default function MeusProfessoresScreen() {
  const estado = useCarregamentoVinculos();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Meus Professores" />
      <View
        className="w-full flex-1 self-center gap-four px-four py-four"
        style={{ maxWidth: MaxContentWidth }}
      >
        {estado.status === 'carregando' ? (
          <ActivityIndicator accessibilityLabel="Carregando" />
        ) : estado.status === 'falha' ? (
          <View className="flex-1 items-center justify-center gap-four">
            <ErrorMessage>{estado.mensagem}</ErrorMessage>
            <Button label="Tentar novamente" onPress={estado.tentarNovamente} />
          </View>
        ) : estado.professores.length === 0 ? (
          <ErrorMessage>{MensagemNenhumProfessor}</ErrorMessage>
        ) : (
          <FlatList
            data={estado.professores}
            keyExtractor={(item) => item.professorId}
            renderItem={({ item }) => <ProfessorVinculadoCard professor={item} />}
            contentContainerClassName="gap-two"
            style={{ minHeight: 420, flexGrow: 1 }}
          />
        )}
      </View>
    </SafeAreaView>
  );
}

type EstadoCarregamento =
  | { status: 'carregando' }
  | { status: 'falha'; mensagem: string; tentarNovamente: () => void }
  | { status: 'carregado'; professores: VinculoProfessor[] };

function useCarregamentoVinculos(): EstadoCarregamento {
  const [resultado, setResultado] = useState<
    | { sucesso: true; professores: VinculoProfessor[] }
    | { sucesso: false; mensagem: string }
    | undefined
  >(undefined);
  const [tentativa, setTentativa] = useState(0);
  const tentarNovamente = () => {
    setResultado(undefined);
    setTentativa((atual) => atual + 1);
  };

  useEffect(() => {
    let cancelado = false;
    listarVinculosAluno().then((res) => {
      if (cancelado) return;
      setResultado(res.sucesso ? { sucesso: true, professores: res.vinculos } : { sucesso: false, mensagem: res.mensagem });
    });
    return () => {
      cancelado = true;
    };
  }, [tentativa]);

  if (resultado === undefined) return { status: 'carregando' };
  if (!resultado.sucesso) return { status: 'falha', mensagem: resultado.mensagem, tentarNovamente };
  return { status: 'carregado', professores: resultado.professores };
}
