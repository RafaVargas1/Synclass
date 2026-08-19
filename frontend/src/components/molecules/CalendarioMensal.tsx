import { Pressable, Text, View } from 'react-native';

import { paraDataISO } from '@/lib/formatarData';

export type CalendarioMensalProps = {
  mesReferencia: Date;
  dataSelecionada?: string;
  onSelecionarDia: (dataISO: string) => void;
  onMudarMes: (novoMes: Date) => void;
};

const NomesDosMeses = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
];
const DiasDaSemana = ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'];

/**
 * Molécula: grade de calendário de um mês, usada pelo seletor de data do
 * filtro de período (issue de usabilidade — campos de data eram texto cru
 * `yyyy-MM-dd` digitado à mão). Semanas incompletas no início/fim do mês
 * ficam com célula vazia (`null`), não com dia do mês vizinho, pra não
 * criar a falsa impressão de que dá pra selecionar fora do mês mostrado.
 */
export function CalendarioMensal({ mesReferencia, dataSelecionada, onSelecionarDia, onMudarMes }: CalendarioMensalProps) {
  const ano = mesReferencia.getFullYear();
  const mes = mesReferencia.getMonth();

  return (
    <View className="w-full gap-three border border-border bg-background-element p-three dark:border-dark-border dark:bg-dark-background-element">
      <CabecalhoDoMes ano={ano} mes={mes} onMudarMes={onMudarMes} />
      <LinhaDosDiasDaSemana />
      <GradeDeDias ano={ano} mes={mes} dataSelecionada={dataSelecionada} onSelecionarDia={onSelecionarDia} />
    </View>
  );
}

function CabecalhoDoMes({ ano, mes, onMudarMes }: { ano: number; mes: number; onMudarMes: (novoMes: Date) => void }) {
  return (
    <View className="flex-row items-center justify-between">
      <SetaDeMes label="Mês anterior" direcao={-1} onPress={() => onMudarMes(new Date(ano, mes - 1, 1))} />
      <Text className="text-sm font-semibold uppercase tracking-widest text-text dark:text-dark-text">
        {NomesDosMeses[mes]} de {ano}
      </Text>
      <SetaDeMes label="Próximo mês" direcao={1} onPress={() => onMudarMes(new Date(ano, mes + 1, 1))} />
    </View>
  );
}

function SetaDeMes({ label, direcao, onPress }: { label: string; direcao: -1 | 1; onPress: () => void }) {
  return (
    <Pressable accessibilityRole="button" accessibilityLabel={label} onPress={onPress} hitSlop={8}>
      <View
        className="border-l-2 border-t-2 border-text dark:border-dark-text"
        style={{ width: 9, height: 9, transform: [{ rotate: direcao === -1 ? '-45deg' : '135deg' }] }}
      />
    </Pressable>
  );
}

function LinhaDosDiasDaSemana() {
  return (
    <View className="flex-row">
      {DiasDaSemana.map((letra, indice) => (
        <Text
          key={indice}
          className="flex-1 text-center text-xs font-semibold text-text-secondary dark:text-dark-text-secondary"
        >
          {letra}
        </Text>
      ))}
    </View>
  );
}

function celulasDoMes(ano: number, mes: number): (number | null)[] {
  const diasNoMes = new Date(ano, mes + 1, 0).getDate();
  const offsetInicial = new Date(ano, mes, 1).getDay();
  const celulas: (number | null)[] = Array(offsetInicial).fill(null);
  for (let dia = 1; dia <= diasNoMes; dia += 1) {
    celulas.push(dia);
  }
  while (celulas.length % 7 !== 0) {
    celulas.push(null);
  }
  return celulas;
}

function GradeDeDias({
  ano,
  mes,
  dataSelecionada,
  onSelecionarDia,
}: {
  ano: number;
  mes: number;
  dataSelecionada: string | undefined;
  onSelecionarDia: (dataISO: string) => void;
}) {
  const celulas = celulasDoMes(ano, mes);

  return (
    <View className="flex-row flex-wrap">
      {celulas.map((dia, indice) => (
        <CelulaDoDia
          key={indice}
          ano={ano}
          mes={mes}
          dia={dia}
          dataSelecionada={dataSelecionada}
          onSelecionarDia={onSelecionarDia}
        />
      ))}
    </View>
  );
}

function CelulaDoDia({
  ano,
  mes,
  dia,
  dataSelecionada,
  onSelecionarDia,
}: {
  ano: number;
  mes: number;
  dia: number | null;
  dataSelecionada: string | undefined;
  onSelecionarDia: (dataISO: string) => void;
}) {
  if (dia === null) {
    return <View className="aspect-square w-[14.28%]" />;
  }

  const dataISO = paraDataISO(new Date(ano, mes, dia));
  const selecionado = dataISO === dataSelecionada;

  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected: selecionado }}
      className={`aspect-square w-[14.28%] items-center justify-center ${selecionado ? 'bg-primary dark:bg-dark-primary' : ''}`}
      onPress={() => onSelecionarDia(dataISO)}
    >
      <Text className={`text-sm ${selecionado ? 'font-bold text-white' : 'text-text dark:text-dark-text'}`}>
        {dia}
      </Text>
    </Pressable>
  );
}
