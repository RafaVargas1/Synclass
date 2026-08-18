import { Text, View } from 'react-native';

import type { ValorDevidoPorMatricula } from '@/lib/api/valorDevido';

export type ValorDevidoCardProps = {
  valorDevido: ValorDevidoPorMatricula;
};

const RotuloSemRegra = 'Sem regra de cobrança definida';

/**
 * Organismo: item da lista de valor devido por Aluno (issue #12). Mesmo
 * padrão visual de `HorarioCard` — `valorDevido.valor` é sempre `null`
 * quando `semRegraDefinida` é `true` (nunca `0`, critério de aceite 3),
 * exibido como texto explícito em vez de "R$ 0,00".
 */
export function ValorDevidoCard({ valorDevido }: ValorDevidoCardProps) {
  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-base font-semibold text-text dark:text-dark-text">{valorDevido.nome}</Text>
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
        {formatarRotuloValor(valorDevido)}
      </Text>
    </View>
  );
}

function formatarRotuloValor(valorDevido: ValorDevidoPorMatricula): string {
  if (valorDevido.semRegraDefinida || valorDevido.valor === null) {
    return RotuloSemRegra;
  }
  return valorDevido.valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
