import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { FlatList, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { AulaProximaCard } from '@/components/organisms/AulaProximaCard';
import { cancelarAula, listarProximasAulas, type AulaProxima } from '@/lib/api/cancelamentos';

const MensagemNenhumaAulaProxima = 'Você ainda não tem nenhuma aula marcada.';

/**
 * Tela do Aluno para listar e cancelar as próximas aulas com um Professor
 * (issue #10) — rota irmã de `horarios.tsx` (issue #9), mesmo padrão de
 * segmentos: separada porque são ações opostas (marcar um vago vs. cancelar
 * um já marcado) sobre listas diferentes (vagos vs. próprias alocações).
 * `professorId` continua vindo da rota (identifica o Professor sendo
 * navegado, não é a identidade do Aluno) — `matriculaId` nunca chega ao
 * cliente (issue #23): a Api resolve a matrícula do Aluno autenticado a
 * partir do token da sessão.
 */
export default function MinhasAulasAlunoScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const estado = useGerenciamentoProximasAulas(professorId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        <Heading level={1} accessibilityRole="header">Minhas aulas</Heading>
        {estado.erro ? <ErrorMessage>{estado.erro}</ErrorMessage> : null}
        <ConteudoProximasAulas aulas={estado.aulas} onCancelar={estado.handleCancelar} />
      </View>
    </SafeAreaView>
  );
}

function ConteudoProximasAulas({
  aulas,
  onCancelar,
}: {
  aulas: AulaProxima[];
  onCancelar: (horarioId: string, data: string) => void;
}) {
  if (aulas.length === 0) {
    return (
      <Text className="text-text-secondary dark:text-dark-text-secondary">
        {MensagemNenhumaAulaProxima}
      </Text>
    );
  }

  return (
    <FlatList
      data={aulas}
      keyExtractor={(item) => item.horarioId}
      renderItem={({ item }) => <AulaProximaCard aulaProxima={item} onCancelar={onCancelar} />}
      contentContainerClassName="gap-two"
    />
  );
}

/**
 * Carrega as próximas aulas ao montar e expõe o handler de cancelar — mesmo
 * padrão de `horarios.tsx#useGerenciamentoHorariosVagos` (issue #9).
 */
function useGerenciamentoProximasAulas(professorId: string) {
  const [aulas, setAulas] = useState<AulaProxima[]>([]);
  const [erro, setErro] = useState<string | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    carregarProximasAulas(professorId, setAulas, setErro, () => cancelado);
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  const handleCancelar = criarHandleCancelar(professorId, setAulas, setErro);
  return { aulas, erro, handleCancelar };
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

function criarHandleCancelar(
  professorId: string,
  setAulas: (aulas: AulaProxima[]) => void,
  setErro: (mensagem: string | undefined) => void,
) {
  return async (horarioId: string, data: string) => {
    setErro(undefined);
    const resultado = await cancelarAula(professorId, horarioId, data);
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
