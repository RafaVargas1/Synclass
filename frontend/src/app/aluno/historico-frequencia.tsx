import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Paragraph } from '@/components/atoms/Paragraph';
import { SeletorDePeriodo } from '@/components/molecules/SeletorDePeriodo';
import { HistoricoFrequenciaCard } from '@/components/organisms/HistoricoFrequenciaCard';
import { Topbar } from '@/components/organisms/Topbar';
import {
  listarHistoricoFrequenciaDoAluno,
  type HistoricoFrequenciaPorProfessor,
  type ListarHistoricoFrequenciaResultado,
  type PeriodoConsultaInput,
} from '@/lib/api/historicoFrequencia';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de consulta do histórico de frequência do Aluno autenticado,
 * detalhado por Professor (issue #16) — reaproveita `SeletorDePeriodo`
 * extraído de `valor-devido.tsx` (issue #13) e o organism novo
 * `HistoricoFrequenciaCard`. Sem segmento de rota (`[professorId]`), mesmo
 * padrão de `aluno/valor-devido.tsx`: `alunoUsuarioId` vem do token da
 * sessão, a Api já devolve a lista agrupada por Professor.
 */
export default function HistoricoFrequenciaAlunoScreen() {
  const estado = useConsultaHistoricoFrequenciaDoAluno();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Meu histórico de frequência" />
      <View className="w-full flex-1 self-center gap-four px-four py-four" style={{ maxWidth: MaxContentWidth }}>
        <SeletorDePeriodo
          inicio={estado.inicio}
          fim={estado.fim}
          onChangeInicio={estado.setInicio}
          onChangeFim={estado.setFim}
          onConsultar={estado.consultar}
        />
        {estado.carregando && <TelaCarregando />}
        {!estado.carregando && estado.erro && <ErrorMessage>{estado.erro}</ErrorMessage>}
        {!estado.carregando && !estado.erro && <ListaDeHistoricos historico={estado.historico} />}
      </View>
    </SafeAreaView>
  );
}

function ListaDeHistoricos({ historico }: { historico: HistoricoFrequenciaPorProfessor[] }) {
  const semAulas = historico.every((porProfessor) => porProfessor.aulas.length === 0);

  if (semAulas) {
    return <Paragraph>Nenhum histórico de frequência para este período.</Paragraph>;
  }

  return (
    <FlatList
      data={historico}
      keyExtractor={(item) => item.professorId}
      renderItem={({ item }) => <HistoricoPorProfessor historico={item} />}
      contentContainerClassName="gap-four"
    />
  );
}

function HistoricoPorProfessor({ historico }: { historico: HistoricoFrequenciaPorProfessor }) {
  return (
    <View className="gap-two">
      <Paragraph className="font-semibold">{historico.nomeProfessor}</Paragraph>
      <FlatList
        data={historico.aulas}
        keyExtractor={(aula) => `${historico.professorId}-${aula.horarioId}-${aula.data}`}
        renderItem={({ item }) => <HistoricoFrequenciaCard aula={item} />}
        contentContainerClassName="gap-two"
        scrollEnabled={false}
      />
    </View>
  );
}

function TelaCarregando() {
  return <ActivityIndicator accessibilityLabel="Carregando" />;
}

/**
 * Carrega o histórico ao montar (mês corrente, `periodo` indefinido) e
 * expõe `consultar` para recarregar com o período digitado — mesma
 * estratégia de `aluno/valor-devido.tsx#useConsultaValorDevidoDoAluno`.
 */
function useConsultaHistoricoFrequenciaDoAluno() {
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [periodo, setPeriodo] = useState<PeriodoConsultaInput | undefined>(undefined);
  const [resultado, setResultado] = useState<ListarHistoricoFrequenciaResultado | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    listarHistoricoFrequenciaDoAluno(periodo).then((res) => {
      if (!cancelado) setResultado(res);
    });
    return () => {
      cancelado = true;
    };
  }, [periodo]);

  const consultar = () => {
    setResultado(undefined);
    setPeriodo(inicio && fim ? { inicio, fim } : undefined);
  };

  return { inicio, setInicio, fim, setFim, consultar, ...derivarEstadoConsulta(resultado) };
}

function derivarEstadoConsulta(resultado: ListarHistoricoFrequenciaResultado | undefined) {
  return {
    carregando: resultado === undefined,
    erro: resultado && !resultado.sucesso ? resultado.mensagem : undefined,
    historico: resultado && resultado.sucesso ? resultado.historico : [],
  };
}
