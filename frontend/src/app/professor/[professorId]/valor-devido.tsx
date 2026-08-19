import { useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { ActivityIndicator, FlatList, Pressable, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { Paragraph } from '@/components/atoms/Paragraph';
import { SeletorDeData } from '@/components/molecules/SeletorDeData';
import { Topbar } from '@/components/organisms/Topbar';
import { ValorDevidoCard } from '@/components/organisms/ValorDevidoCard';
import {
  calcularPeriodoTodos,
  listarValorDevido,
  type ListarValorDevidoResultado,
  type PeriodoConsultaInput,
  type ValorDevidoPorMatricula,
} from '@/lib/api/valorDevido';
import { proximoDia } from '@/lib/formatarData';
import { MaxContentWidth } from '@/theme/tokens';

type Modo = 'todos' | 'mes' | 'personalizado';

/**
 * Tela de consulta do valor devido por Aluno (issue #12, revisitada por
 * problema de usabilidade real: sem período informado a Api caía no mês
 * corrente, dando a falsa impressão de "nenhum Aluno" quando só não havia
 * cobrança nesse mês). Agora lista TODOS por padrão (`calcularPeriodoTodos`),
 * com "Este mês" e "Personalizado" como filtros explícitos — o período
 * personalizado usa um calendário de verdade em vez de `yyyy-MM-dd` digitado.
 */
export default function ValorDevidoScreen() {
  const { professorId } = useLocalSearchParams<{ professorId: string }>();
  const estado = useConsultaValorDevido(professorId);

  return (
    <SafeAreaView className="flex-1 bg-background dark:bg-dark-background">
      <Topbar titulo="Valor devido por Aluno" />
      <View className="w-full flex-1 self-center gap-four px-four py-five" style={{ maxWidth: MaxContentWidth }}>
        <FiltroDePeriodo modo={estado.modo} onMudarModo={estado.setModo} />
        {estado.modo === 'personalizado' ? (
          <PeriodoPersonalizado
            inicio={estado.inicioP}
            fim={estado.fimP}
            onSelecionarInicio={estado.setInicioP}
            onSelecionarFim={estado.setFimP}
          />
        ) : null}
        <ConteudoDaConsulta estado={estado} />
      </View>
    </SafeAreaView>
  );
}

function FiltroDePeriodo({ modo, onMudarModo }: { modo: Modo; onMudarModo: (modo: Modo) => void }) {
  const opcoes: { valor: Modo; label: string }[] = [
    { valor: 'todos', label: 'Todos' },
    { valor: 'mes', label: 'Este mês' },
    { valor: 'personalizado', label: 'Personalizado' },
  ];

  return (
    <View accessibilityRole="tablist" className="flex-row self-start border border-text dark:border-dark-text">
      {opcoes.map((opcao) => (
        <ChipDeModo key={opcao.valor} label={opcao.label} selecionado={modo === opcao.valor} onPress={() => onMudarModo(opcao.valor)} />
      ))}
    </View>
  );
}

function ChipDeModo({ label, selecionado, onPress }: { label: string; selecionado: boolean; onPress: () => void }) {
  const fundo = selecionado ? 'bg-text dark:bg-dark-text' : 'bg-transparent';
  const texto = selecionado ? 'text-background dark:text-dark-background' : 'text-text dark:text-dark-text';

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      onPress={onPress}
      className={`px-three py-two ${fundo}`}
    >
      <Text className={`text-sm font-semibold ${texto}`}>{label}</Text>
    </Pressable>
  );
}

function PeriodoPersonalizado({
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

function ConteudoDaConsulta({ estado }: { estado: EstadoConsulta }) {
  if (!estado.pronto) {
    return <Paragraph>Selecione o início e o fim do período.</Paragraph>;
  }
  if (estado.carregando) {
    return <ActivityIndicator accessibilityLabel="Carregando" />;
  }
  if (estado.erro) {
    return <ErrorMessage>{estado.erro}</ErrorMessage>;
  }
  return <ListaDeValoresDevidos valoresDevidos={estado.valoresDevidos} />;
}

function ErrorMessage({ children }: { children: string }) {
  return <Paragraph className="text-error dark:text-dark-error">{children}</Paragraph>;
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

type EstadoConsulta = {
  modo: Modo;
  setModo: (modo: Modo) => void;
  inicioP: string | undefined;
  setInicioP: (dataISO: string) => void;
  fimP: string | undefined;
  setFimP: (dataISO: string) => void;
  pronto: boolean;
  carregando: boolean;
  erro: string | undefined;
  valoresDevidos: ValorDevidoPorMatricula[];
};

function periodoDoModo(modo: Modo, inicioP: string | undefined, fimP: string | undefined): PeriodoConsultaInput | undefined {
  if (modo === 'todos') {
    return calcularPeriodoTodos(new Date());
  }
  if (modo === 'personalizado' && inicioP && fimP) {
    return { inicio: inicioP, fim: proximoDia(fimP) };
  }
  return undefined;
}

/**
 * `resultado` guarda a chave da consulta que ele responde (`chaveAtual`),
 * não só os dados — assim `carregando` é derivado comparando chaves em vez
 * de zerar `resultado` de forma síncrona no corpo do efeito (mesma
 * preocupação de `react-hooks/set-state-in-effect` já documentada em
 * `painel/index.tsx#useUsuarioIdLogado`).
 */
function useConsultaValorDevido(professorId: string) {
  const [modo, setModo] = useState<Modo>('todos');
  const [inicioP, setInicioP] = useState<string | undefined>(undefined);
  const [fimP, setFimP] = useState<string | undefined>(undefined);
  const [resultado, setResultado] = useState<{ chave: string; dados: ListarValorDevidoResultado } | undefined>(undefined);

  const pronto = modo !== 'personalizado' || (inicioP !== undefined && fimP !== undefined);
  const periodo = periodoDoModo(modo, inicioP, fimP);
  const chaveAtual = `${modo}|${periodo?.inicio ?? ''}|${periodo?.fim ?? ''}`;

  useEffect(() => {
    if (!pronto) {
      return;
    }
    let cancelado = false;
    listarValorDevido(professorId, periodo).then((dados) => {
      if (!cancelado) setResultado({ chave: chaveAtual, dados });
    });
    return () => {
      cancelado = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- `periodo` é derivado de `chaveAtual`, incluir os dois duplicaria a dependência
  }, [professorId, pronto, chaveAtual]);

  const carregando = pronto && (resultado === undefined || resultado.chave !== chaveAtual);
  const dadosAtuais = resultado?.chave === chaveAtual ? resultado.dados : undefined;

  return {
    modo,
    setModo,
    inicioP,
    setInicioP,
    fimP,
    setFimP,
    pronto,
    carregando,
    erro: dadosAtuais && !dadosAtuais.sucesso ? dadosAtuais.mensagem : undefined,
    valoresDevidos: dadosAtuais?.sucesso ? dadosAtuais.valoresDevidos : [],
  };
}
