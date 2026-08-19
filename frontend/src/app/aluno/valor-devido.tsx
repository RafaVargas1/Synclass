import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { Paragraph } from '@/components/atoms/Paragraph';
import { SeletorDePeriodo } from '@/components/molecules/SeletorDePeriodo';
import { ValorDevidoCard } from '@/components/organisms/ValorDevidoCard';
import {
  listarValorDevidoDoAluno,
  type ListarValorDevidoResultado,
  type PeriodoConsultaInput,
  type ValorDevidoPorMatricula,
} from '@/lib/api/valorDevido';

/**
 * Tela de consulta do total devido pelo Aluno autenticado, detalhado por
 * Professor (issue #13) — reaproveita `ValorDevidoCard`/`listarValorDevidoDoAluno`
 * criados pela issue #12. Sem segmento de rota (`[professorId]`), diferente
 * de `professor/[professorId]/valor-devido.tsx`: a lista já é agregada de
 * todos os Professores do Aluno, sem total somado entre eles (RN da #13) —
 * a tela só renderiza a lista, sem nenhum `reduce`/soma.
 */
export default function ValorDevidoAlunoScreen() {
  const estado = useConsultaValorDevidoDoAluno();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        <Heading level={1}>Quanto tenho que pagar</Heading>
        <SeletorDePeriodo
          inicio={estado.inicio}
          fim={estado.fim}
          onChangeInicio={estado.setInicio}
          onChangeFim={estado.setFim}
          onConsultar={estado.consultar}
        />
        {estado.carregando && <TelaCarregando />}
        {!estado.carregando && estado.erro && <ErrorMessage>{estado.erro}</ErrorMessage>}
        {!estado.carregando && !estado.erro && <ListaDeValoresDevidos valoresDevidos={estado.valoresDevidos} />}
      </View>
    </SafeAreaView>
  );
}

function ListaDeValoresDevidos({ valoresDevidos }: { valoresDevidos: ValorDevidoPorMatricula[] }) {
  if (valoresDevidos.length === 0) {
    return <Paragraph>Nenhuma cobrança ativa ou pendente para este período.</Paragraph>;
  }

  return (
    <FlatList
      data={valoresDevidos}
      keyExtractor={(item) => item.matriculaId}
      renderItem={({ item }) => <ValorDevidoCard valorDevido={item} />}
      contentContainerClassName="gap-two"
    />
  );
}

function TelaCarregando() {
  return <ActivityIndicator accessibilityLabel="Carregando" />;
}

/**
 * Carrega o valor devido ao montar (mês corrente, `periodo` indefinido) e
 * expõe `consultar` para recarregar com o período digitado — mesma
 * estratégia de `professor/[professorId]/valor-devido.tsx#useConsultaValorDevido`,
 * só sem `professorId` (a Api resolve o Aluno pelo token da sessão).
 */
function useConsultaValorDevidoDoAluno() {
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [periodo, setPeriodo] = useState<PeriodoConsultaInput | undefined>(undefined);
  const [resultado, setResultado] = useState<ListarValorDevidoResultado | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    listarValorDevidoDoAluno(periodo).then((res) => {
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

function derivarEstadoConsulta(resultado: ListarValorDevidoResultado | undefined) {
  return {
    carregando: resultado === undefined,
    erro: resultado && !resultado.sucesso ? resultado.mensagem : undefined,
    valoresDevidos: resultado && resultado.sucesso ? resultado.valoresDevidos : [],
  };
}
