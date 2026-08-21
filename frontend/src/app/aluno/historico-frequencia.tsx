import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Paragraph } from '@/components/atoms/Paragraph';
import { SeletorDeData } from '@/components/molecules/SeletorDeData';
import { HistoricoFrequenciaCard } from '@/components/organisms/HistoricoFrequenciaCard';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import {
  listarHistoricoFrequenciaDoAluno,
  type HistoricoFrequenciaPorProfessor,
  type ListarHistoricoFrequenciaResultado,
  type PeriodoConsultaInput,
} from '@/lib/api/historicoFrequencia';
import { proximoDia } from '@/lib/formatarData';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de consulta do histórico de frequência do Aluno autenticado,
 * detalhado por Professor (issue #16) — desde a #116 usa dois
 * `SeletorDeData` (calendário) no lugar de `SeletorDePeriodo` (texto livre +
 * botão "Consultar"), com a consulta reagindo à mudança das duas datas, mesmo
 * padrão reativo de `professor/[professorId]/valor-devido.tsx`. Sem segmento
 * de rota (`[professorId]`), mesmo padrão de `aluno/valor-devido.tsx`:
 * `alunoUsuarioId` vem do token da sessão. Sem estado de seleção inicial,
 * monta carregando o mês corrente (`periodo` indefinido).
 */
export default function HistoricoFrequenciaAlunoScreen() {
  const estado = useConsultaHistoricoFrequenciaDoAluno();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Meu histórico de frequência" />
      <View className="w-full flex-1 self-center gap-four px-four py-four" style={{ maxWidth: MaxContentWidth }}>
        <PeriodoSelecionado
          inicio={estado.inicio}
          fim={estado.fim}
          onSelecionarInicio={estado.setInicio}
          onSelecionarFim={estado.setFim}
        />
        {estado.carregando && <TelaCarregando />}
        {!estado.carregando && estado.erro && <ErrorMessage>{estado.erro}</ErrorMessage>}
        {!estado.carregando && !estado.erro && <ListaDeHistoricos historico={estado.historico} />}
      </View>
    </SafeAreaView>
  );
}

function PeriodoSelecionado({
  inicio,
  fim,
  onSelecionarInicio,
  onSelecionarFim,
}: {
  inicio: string | undefined;
  fim: string | undefined;
  onSelecionarInicio: (dataISO: string) => void;
  onSelecionarFim: (dataISO: string) => void;
}) {
  return (
    <View className="flex-row gap-three">
      <View className="flex-1">
        <SeletorDeData label="Início" valor={inicio} onSelecionar={onSelecionarInicio} />
      </View>
      <View className="flex-1">
        <SeletorDeData label="Fim" valor={fim} onSelecionar={onSelecionarFim} />
      </View>
    </View>
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
 * Carrega o histórico ao montar (mês corrente, período indefinido) e reage à
 * seleção das duas datas no calendário (`SeletorDeData`): `chaveAtual` vira
 * `mes` enquanto não houver início e fim, e o período escolhido assim que as
 * duas estiverem preenchidas. Mesmo padrão de
 * `professor/[professorId]/valor-devido.tsx#useConsultaValorDevido` — o fim
 * escolhido (inclusive) vira `proximoDia` (exclusive) no contrato da Api.
 */
function useConsultaHistoricoFrequenciaDoAluno() {
  const [inicio, setInicio] = useState<string | undefined>(undefined);
  const [fim, setFim] = useState<string | undefined>(undefined);
  const [resultado, setResultado] = useState<{ chave: string; dados: ListarHistoricoFrequenciaResultado } | undefined>(undefined);

  const periodo = inicio && fim ? ({ inicio, fim: proximoDia(fim) } satisfies PeriodoConsultaInput) : undefined;
  const chaveAtual = inicio && fim ? `${inicio}|${fim}` : 'mes';

  useEffect(() => {
    let cancelado = false;
    listarHistoricoFrequenciaDoAluno(periodo).then((dados) => {
      if (!cancelado) setResultado({ chave: chaveAtual, dados });
    });
    return () => {
      cancelado = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- `periodo` é derivado de `chaveAtual`, incluir os dois duplicaria a dependência
  }, [chaveAtual]);

  return {
    inicio,
    setInicio,
    fim,
    setFim,
    ...derivarEstadoConsulta(chaveAtual, resultado),
  };
}

function derivarEstadoConsulta(
  chaveAtual: string,
  resultado: { chave: string; dados: ListarHistoricoFrequenciaResultado } | undefined,
) {
  const dadosAtuais = resultado?.chave === chaveAtual ? resultado.dados : undefined;
  return {
    carregando: dadosAtuais === undefined,
    erro: dadosAtuais && !dadosAtuais.sucesso ? dadosAtuais.mensagem : undefined,
    historico: dadosAtuais?.sucesso ? dadosAtuais.historico : [],
  };
}
