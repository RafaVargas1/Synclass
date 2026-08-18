import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Button } from '@/components/atoms/Button';
import { ErrorMessage } from '@/components/atoms/ErrorMessage';
import { Heading } from '@/components/atoms/Heading';
import { Input } from '@/components/atoms/Input';
import { Paragraph } from '@/components/atoms/Paragraph';
import { ValorDevidoCard } from '@/components/organisms/ValorDevidoCard';
import {
  listarValorDevido,
  type ListarValorDevidoResultado,
  type PeriodoConsultaInput,
  type ValorDevidoPorMatricula,
} from '@/lib/api/valorDevido';

/**
 * Tela de consulta do valor devido por Aluno (issue #12). `professorId` vem
 * da rota, mesmo padrão de `alocacoes.tsx`/`regra-de-cobranca.tsx` — ainda
 * não há sessão logada (issue #18, em paralelo) de onde derivar o Professor
 * autenticado. Sem período informado, carrega o mês corrente (default do
 * backend, ver `lib/api/valorDevido.ts`); o seletor permite consultar um
 * período específico digitando `inicio`/`fim` (yyyy-MM-dd).
 */
export default function ValorDevidoScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const estado = useConsultaValorDevido(professorId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <View className="flex-1 gap-four px-four py-four">
        <Heading level={1}>Valor devido por Aluno</Heading>
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

function SeletorDePeriodo({
  inicio,
  fim,
  onChangeInicio,
  onChangeFim,
  onConsultar,
}: {
  inicio: string;
  fim: string;
  onChangeInicio: (valor: string) => void;
  onChangeFim: (valor: string) => void;
  onConsultar: () => void;
}) {
  return (
    <View className="gap-two">
      <Paragraph>Período (aaaa-mm-dd) — vazio usa o mês corrente</Paragraph>
      <View className="flex-row gap-two">
        <Input
          accessibilityLabel="Início do período"
          placeholder="Início"
          value={inicio}
          onChangeText={onChangeInicio}
          className="flex-1"
        />
        <Input
          accessibilityLabel="Fim do período"
          placeholder="Fim"
          value={fim}
          onChangeText={onChangeFim}
          className="flex-1"
        />
      </View>
      <Button label="Consultar" onPress={onConsultar} />
    </View>
  );
}

function ListaDeValoresDevidos({ valoresDevidos }: { valoresDevidos: ValorDevidoPorMatricula[] }) {
  if (valoresDevidos.length === 0) {
    return <Paragraph>Nenhum Aluno encontrado para este período.</Paragraph>;
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
 * expõe `consultar` para recarregar com o período digitado. `resultado`
 * indefinido é o próprio estado de carregamento (mesma estratégia de
 * `regra-de-cobranca.tsx#useCarregamentoRegra`) — evita chamar `setState`
 * síncrono no corpo do efeito (`react-hooks/set-state-in-effect`), já que
 * `consultar` reseta `resultado` para `undefined` antes de trocar `periodo`.
 */
function useConsultaValorDevido(professorId: string) {
  const [inicio, setInicio] = useState('');
  const [fim, setFim] = useState('');
  const [periodo, setPeriodo] = useState<PeriodoConsultaInput | undefined>(undefined);
  const [resultado, setResultado] = useState<ListarValorDevidoResultado | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    listarValorDevido(professorId, periodo).then((res) => {
      if (!cancelado) setResultado(res);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId, periodo]);

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
