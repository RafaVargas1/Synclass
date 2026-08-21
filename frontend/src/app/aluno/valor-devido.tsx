import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Paragraph } from '@/components/atoms/Paragraph';
import { SeletorDePeriodoDuplo } from '@/components/molecules/SeletorDePeriodoDuplo';
import { TopbarAutenticada } from '@/components/organisms/TopbarAutenticada';
import { ValorDevidoCard } from '@/components/organisms/ValorDevidoCard';
import {
  listarValorDevidoDoAluno,
  type ListarValorDevidoResultado,
  type PeriodoConsultaInput,
  type ValorDevidoPorMatricula,
} from '@/lib/api/valorDevido';
import { proximoDia } from '@/lib/formatarData';
import { MaxContentWidth } from '@/theme/tokens';

/**
 * Tela de consulta do total devido pelo Aluno autenticado, detalhado por
 * Professor (issue #13) — desde a #116 usa dois `SeletorDeData` (calendário)
 * no lugar de `SeletorDePeriodo` (texto livre + botão "Consultar"), com a
 * consulta reagindo à mudança das duas datas, mesmo padrão reativo de
 * `professor/[professorId]/valor-devido.tsx`. Reaproveita
 * `ValorDevidoCard`/`listarValorDevidoDoAluno` criados pela issue #12. Sem
 * segmento de rota (`[professorId]`): a lista já é agregada de todos os
 * Professores do Aluno, sem total somado entre eles (RN da #13) — a tela só
 * renderiza a lista, sem nenhum `reduce`/soma.
 */
export default function ValorDevidoAlunoScreen() {
  const estado = useConsultaValorDevidoDoAluno();

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <TopbarAutenticada titulo="Quanto tenho que pagar" />
      <View className="w-full flex-1 self-center gap-four px-four py-four" style={{ maxWidth: MaxContentWidth }}>
        <SeletorDePeriodoDuplo
          inicio={estado.inicio}
          fim={estado.fim}
          onSelecionarInicio={estado.setInicio}
          onSelecionarFim={estado.setFim}
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
 * Carrega o valor devido ao montar (mês corrente, período indefinido) e reage
 * à seleção das duas datas no calendário (`SeletorDeData`): `chaveAtual` vira
 * `mes` enquanto não houver início e fim, e o período escolhido assim que as
 * duas estiverem preenchidas. Mesmo padrão de
 * `professor/[professorId]/valor-devido.tsx#useConsultaValorDevido` — o fim
 * escolhido (inclusive) vira `proximoDia` (exclusive) no contrato da Api, só
 * sem `professorId` (a Api resolve o Aluno pelo token da sessão).
 */
function useConsultaValorDevidoDoAluno() {
  const [inicio, setInicio] = useState<string | undefined>(undefined);
  const [fim, setFim] = useState<string | undefined>(undefined);
  const [resultado, setResultado] = useState<{ chave: string; dados: ListarValorDevidoResultado } | undefined>(undefined);

  const periodo = inicio && fim ? ({ inicio, fim: proximoDia(fim) } satisfies PeriodoConsultaInput) : undefined;
  const chaveAtual = inicio && fim ? `${inicio}|${fim}` : 'mes';

  useEffect(() => {
    let cancelado = false;
    listarValorDevidoDoAluno(periodo).then((dados) => {
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
  resultado: { chave: string; dados: ListarValorDevidoResultado } | undefined,
) {
  const dadosAtuais = resultado?.chave === chaveAtual ? resultado.dados : undefined;
  return {
    carregando: dadosAtuais === undefined,
    erro: dadosAtuais && !dadosAtuais.sucesso ? dadosAtuais.mensagem : undefined,
    valoresDevidos: dadosAtuais?.sucesso ? dadosAtuais.valoresDevidos : [],
  };
}
