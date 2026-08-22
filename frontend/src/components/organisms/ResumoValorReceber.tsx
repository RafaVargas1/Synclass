import { Text, View } from 'react-native';

/**
 * Organismo de leitura não-tocável (issue #166): card informativo do total
 * a receber no mês corrente, montado no Painel do Professor. Reapresenta um
 * dado que a Api já calculou (`GET /professores/{professorId}/valor-devido`
 * sem período → mês corrente) — nenhuma lógica de cálculo nova aqui. Sendo
 * dado informativo (não ação), é um `View` puro, não um `Pressable`/`Link`:
 * a ação "Ver valor devido" já existe como `CardDeAcao` abaixo, e adicionar
 * área tocável aqui faria o card parecer clicável sem sê-lo (por isso não
 * se aplica `docs/spec/ux-heuristics.md#alvos-de-toque`).
 */
export function ResumoValorReceber({ total }: { total: number }) {
  return (
    <View className="w-full gap-one rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">A receber este mês</Text>
      {total > 0 ? (
        <Text className="text-2xl font-bold text-text dark:text-dark-text">
          {total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
        </Text>
      ) : (
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          Nenhum valor a receber neste mês.
        </Text>
      )}
    </View>
  );
}
